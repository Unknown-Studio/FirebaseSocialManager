using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Firestore;
using Suhdo.FSM.Friends.Models;
using Suhdo.FSM.Core;
using UnityEngine;

namespace Suhdo.FSM.Friends
{
    public class FriendService<T> : IFriendService<T> where T : FriendRecord, new()
    {
        private readonly FirebaseFirestore _db;
        private readonly FirebaseAuth _auth;
        private readonly DataCache<List<T>> _friendsCache;

        public FriendService(FirebaseFirestore firestore, FirebaseAuth auth, TimeSpan? cacheDuration = null)
        {
            _db = firestore;
            _auth = auth;
            _friendsCache = new DataCache<List<T>>(cacheDuration ?? TimeSpan.FromMinutes(5));
        }

        private string CurrentUserId => _auth.CurrentUser?.UserId;
        private CollectionReference GetMyFriendsCollection() => _db.Collection("users").Document(CurrentUserId).Collection("friends");
        private CollectionReference GetTargetFriendsCollection(string targetUid) => _db.Collection("users").Document(targetUid).Collection("friends");

        public async Task<List<T>> FetchAllFriendsAsync(CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(CurrentUserId)) return new List<T>();

            if (!_friendsCache.IsExpired)
            {
                Debug.Log("[FriendService] Trả về danh sách bạn bè từ Cache.");
                return _friendsCache.Data;
            }

            try
            {
                Debug.Log("[FriendService] Đang lấy danh sách bạn bè mới từ Firebase...");
                QuerySnapshot snapshot = await GetMyFriendsCollection().GetSnapshotAsync();
                List<T> results = new List<T>();
                foreach (DocumentSnapshot doc in snapshot.Documents)
                {
                    var record = doc.ConvertTo<T>();
                    record.Uid = doc.Id;
                    results.Add(record);
                }

                _friendsCache.Update(results);

                return results;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FriendService] Lỗi kéo danh sách bạn bè: {ex.Message}");
                return new List<T>();
            }
        }

        public void InvalidateCache()
        {
            _friendsCache.Invalidate();
        }

        public async Task<bool> SendFriendRequestAsync(
            string targetUserId, 
            Action<T> onPopulateTargetRecord = null,
            Action<T> onPopulateMyRecord = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(CurrentUserId) || targetUserId == CurrentUserId) return false;

            try
            {
                DocumentReference myRecordRef = GetMyFriendsCollection().Document(targetUserId);
                
                DocumentSnapshot existingSnap = await myRecordRef.GetSnapshotAsync();
                if (existingSnap.Exists)
                {
                    string currentStatus = existingSnap.GetValue<string>("status");

                    if (currentStatus == "pending_received")
                    {
                        Debug.Log("[FriendService] Đối phương đã mời bạn trước đó, hệ thống sẽ tự động Accept kết bạn chéo!");
                        return await RespondToFriendRequestAsync(targetUserId, true, cancellationToken);
                    }
                    
                    if (currentStatus == "pending_sent" || currentStatus == "accepted")
                    {
                        Debug.Log("[FriendService] Yêu cầu xin kết bạn đã tồn tại từ trước.");
                        return false; 
                    }
                }

                WriteBatch batch = _db.StartBatch();

                // 1. Phía mình thiết lập đối phương là pending_sent
                var myRecordData = new T
                {
                    Status = "pending_sent",
                    UpdatedAt = FieldValue.ServerTimestamp
                };
                onPopulateTargetRecord?.Invoke(myRecordData);
                batch.Set(myRecordRef, myRecordData, SetOptions.MergeAll);

                // 2. Phía bạn kia phát hiện có ng add sẽ nhận pending_received
                DocumentReference theirRecordRef = GetTargetFriendsCollection(targetUserId).Document(CurrentUserId);
                var theirRecordData = new T
                {
                    Status = "pending_received",
                    UpdatedAt = FieldValue.ServerTimestamp
                };
                onPopulateMyRecord?.Invoke(theirRecordData);
                batch.Set(theirRecordRef, theirRecordData, SetOptions.MergeAll);

                await batch.CommitAsync();
                InvalidateCache();

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FriendService] Lỗi gửi kết bạn bằng Batch: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RespondToFriendRequestAsync(string targetUserId, bool isAccepted, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(CurrentUserId)) return false;

            try
            {
                WriteBatch batch = _db.StartBatch();
                DocumentReference myRecordRef = GetMyFriendsCollection().Document(targetUserId);
                DocumentReference theirRecordRef = GetTargetFriendsCollection(targetUserId).Document(CurrentUserId);

                if (isAccepted)
                {
                    var updates = new Dictionary<string, object>
                    {
                        { "status", "accepted" },
                        { "updatedAt", FieldValue.ServerTimestamp }
                    };
                    batch.Update(myRecordRef, updates);
                    batch.Update(theirRecordRef, updates);
                }
                else
                {
                    batch.Delete(myRecordRef);
                    batch.Delete(theirRecordRef);
                }

                await batch.CommitAsync();
                InvalidateCache();
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FriendService] Lỗi phản hồi kết bạn: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RemoveFriendAsync(string targetUserId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(CurrentUserId)) return false;

            try
            {
                WriteBatch batch = _db.StartBatch();
                DocumentReference myRecordRef = GetMyFriendsCollection().Document(targetUserId);
                DocumentReference theirRecordRef = GetTargetFriendsCollection(targetUserId).Document(CurrentUserId);

                batch.Delete(myRecordRef);
                batch.Delete(theirRecordRef);

                await batch.CommitAsync();
                InvalidateCache();
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FriendService] Lỗi hủy kết bạn đôi: {ex.Message}");
                return false;
            }
        }

        public IDisposable ListenForFriendRequests(Action<int> onCountChanged)
        {
            if (string.IsNullOrEmpty(CurrentUserId)) return null;

            Query query = GetMyFriendsCollection().WhereEqualTo("status", "pending_received");

            Action<QuerySnapshot> listener = (snapshot) =>
            {
                if (snapshot == null) return;
                onCountChanged?.Invoke(snapshot.Count);
            };

            var listenerRegistration = query.Listen(listener);
            return new ListenerDisposer(listenerRegistration);
        }

        private class ListenerDisposer : IDisposable
        {
            private ListenerRegistration _registration;
            public ListenerDisposer(ListenerRegistration registration) => _registration = registration;
            public void Dispose()
            {
                _registration?.Stop();
                _registration = null;
            }
        }
    }
}
