using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Firestore;
using Suhdo.FSM.Profile.Models;
using Suhdo.FSM.Friends.Models;
using UnityEngine;
using Models_UserProfile = Suhdo.FSM.Profile.Models.UserProfile;
using UserProfile = Suhdo.FSM.Profile.Models.UserProfile;

namespace Suhdo.FSM.Friends
{
    public class FriendService : IFriendService
    {
        private readonly FirebaseFirestore _db;
        private readonly FirebaseAuth _auth;

        public FriendService(FirebaseFirestore firestore, FirebaseAuth auth)
        {
            _db = firestore;
            _auth = auth;
        }

        private string CurrentUserId => _auth.CurrentUser?.UserId;
        private CollectionReference GetMyFriendsCollection() => _db.Collection("users").Document(CurrentUserId).Collection("friends");
        private CollectionReference GetTargetFriendsCollection(string targetUid) => _db.Collection("users").Document(targetUid).Collection("friends");

        public async Task<List<FriendRecord>> FetchAllFriendsAsync(CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(CurrentUserId)) return new List<FriendRecord>();

            try
            {
                QuerySnapshot snapshot = await GetMyFriendsCollection().GetSnapshotAsync();
                List<FriendRecord> results = new List<FriendRecord>();
                foreach (DocumentSnapshot doc in snapshot.Documents)
                {
                    var record = doc.ConvertTo<FriendRecord>();
                    record.Uid = doc.Id;
                    results.Add(record);
                }
                return results;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FriendService] Lỗi kéo danh sách bạn bè: {ex.Message}");
                return new List<FriendRecord>();
            }
        }

        public async Task<bool> SendFriendRequestAsync(string targetUserId, Models_UserProfile targetProfile, Models_UserProfile myProfile, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(CurrentUserId) || targetUserId == CurrentUserId) return false;

            try
            {
                DocumentReference myRecordRef = GetMyFriendsCollection().Document(targetUserId);
                
                // Kiểm tra xem đã có bản ghi tương tác nào giữa 2 người chưa (Ngăn chặn A và B gửi yêu cầu đè lên nhau)
                DocumentSnapshot existingSnap = await myRecordRef.GetSnapshotAsync();
                if (existingSnap.Exists)
                {
                    string currentStatus = existingSnap.GetValue<string>("status");

                    // Nếu đối tượng đã gửi cho mình trước (mình có pending_received), thì thao tác AddFriend của mình được tính là Bấm Đồng Ý!
                    if (currentStatus == "pending_received")
                    {
                        Debug.Log("[FriendService] Đối phương đã mời bạn trước đó, hệ thống sẽ tự động Accept kết bạn chéo!");
                        return await RespondToFriendRequestAsync(targetUserId, true, cancellationToken);
                    }
                    
                    // Chặn hành spam nút gửi khi đã có yêu cầu hoặc đã là bạn rồi
                    if (currentStatus == "pending_sent" || currentStatus == "accepted")
                    {
                        Debug.Log("[FriendService] Yêu cầu xin kết bạn đã tồn tại từ trước.");
                        return false; 
                    }
                }

                // Nếu Profile hoàn toàn mới, dùng WriteBatch để tạo nhánh yêu cầu an toàn 2 chiều
                WriteBatch batch = _db.StartBatch();

                // 1. Phía mình thiết lập đối phương là pending_sent
                var myRecordData = new FriendRecord
                {
                    Status = "pending_sent",
                    FriendName = targetProfile.DisplayName,
                    AvatarId = targetProfile.AvatarId,
                    FrameId = targetProfile.FrameId,
                    UpdatedAt = FieldValue.ServerTimestamp
                };
                batch.Set(myRecordRef, myRecordData, SetOptions.MergeAll);

                // 2. Phía bạn kia phát hiện có ng add sẽ nhận pending_received
                DocumentReference theirRecordRef = GetTargetFriendsCollection(targetUserId).Document(CurrentUserId);
                var theirRecordData = new FriendRecord
                {
                    Status = "pending_received",
                    FriendName = myProfile.DisplayName,
                    AvatarId = myProfile.AvatarId,
                    FrameId = myProfile.FrameId,
                    UpdatedAt = FieldValue.ServerTimestamp
                };
                batch.Set(theirRecordRef, theirRecordData, SetOptions.MergeAll);

                // Gửi toàn bộ lệnh lên chốt sổ Data
                await batch.CommitAsync();
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
                    // Nếu từ chối thì xóa lun mối liên kết tạm thời để nhẹ Data
                    batch.Delete(myRecordRef);
                    batch.Delete(theirRecordRef);
                }

                await batch.CommitAsync();
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
