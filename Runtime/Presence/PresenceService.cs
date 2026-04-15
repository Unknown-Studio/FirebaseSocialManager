using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Database;
using SocialManager.Presence.Models;
using UnityEngine;

namespace SocialManager.Presence
{
    public class PresenceService : IPresenceService
    {
        private const string PATH_PRESENCE = "presence";
        
        private readonly FirebaseDatabase _db;
        private readonly FirebaseAuth _auth;

        public PresenceService(FirebaseDatabase db, FirebaseAuth auth)
        {
            _db = db;
            _auth = auth;
        }

        private string CurrentUserId => _auth.CurrentUser?.UserId;
        private DatabaseReference MyPresenceRef => _db.GetReference(PATH_PRESENCE).Child(CurrentUserId);

        public async Task SetOnlineAsync(CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(CurrentUserId)) return;

            try
            {
                // 1. Thiết lập OnDisconnect: Khi mất kết nối, Server RTDB tự động đổi trạng thái
                var offlineUpdate = new Dictionary<string, object>
                {
                    { "state", "offline" },
                    { "lastChanged", ServerValue.Timestamp }
                };
                await MyPresenceRef.OnDisconnect().UpdateChildren(offlineUpdate);

                // 2. Thiết lập trạng thái Online hiện tại
                var onlineUpdate = new Dictionary<string, object>
                {
                    { "state", "online" },
                    { "lastChanged", ServerValue.Timestamp }
                };
                await MyPresenceRef.UpdateChildrenAsync(onlineUpdate);
                
                Debug.Log($"[PresenceService] User {CurrentUserId} is now ONLINE.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PresenceService] Lỗi khi set online: {ex.Message}");
            }
        }

        public async Task SetOfflineAsync(CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(CurrentUserId)) return;

            try
            {
                var offlineUpdate = new Dictionary<string, object>
                {
                    { "state", "offline" },
                    { "lastChanged", ServerValue.Timestamp }
                };
                await MyPresenceRef.UpdateChildrenAsync(offlineUpdate);
                
                // Hủy OnDisconnect vì chúng ta đã chủ động set offline
                await MyPresenceRef.OnDisconnect().Cancel();
                
                Debug.Log($"[PresenceService] User {CurrentUserId} is now OFFLINE.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PresenceService] Lỗi khi set offline: {ex.Message}");
            }
        }

        public async Task<Dictionary<string, UserPresence>> GetStatusesAsync(IEnumerable<string> userIds, CancellationToken cancellationToken = default)
        {
            var results = new Dictionary<string, UserPresence>();
            if (userIds == null) return results;

            try
            {
                // Vì RTDB không hỗ trợ query "WhereIn" mạnh như Firestore, 
                // cách tối ưu nhất là fetch từng cái hoặc fetch toàn bộ node và lọc ở Client 
                // (nếu danh sách bạn bè không quá lớn).
                // Ở đây ta dùng giải pháp fetch song song từng UserID để đảm bảo tốc độ.
                
                var tasks = new List<Task<(string uid, UserPresence presence)>>();
                foreach (var uid in userIds)
                {
                    tasks.Add(FetchSingleStatus(uid));
                }

                var presences = await Task.WhenAll(tasks);
                foreach (var p in presences)
                {
                    if (p.presence != null)
                    {
                        results[p.uid] = p.presence;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PresenceService] Lỗi khi lấy danh sách trạng thái: {ex.Message}");
            }

            return results;
        }

        private async Task<(string uid, UserPresence presence)> FetchSingleStatus(string uid)
        {
            try
            {
                var snapshot = await _db.GetReference(PATH_PRESENCE).Child(uid).GetValueAsync();
                if (snapshot.Exists)
                {
                    // Chuyển đổi JSON object từ RTDB sang C# Object
                    string json = snapshot.GetRawJsonValue();
                    return (uid, JsonUtility.FromJson<UserPresence>(json));
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PresenceService] Không thể lấy trạng thái cho {uid}: {ex.Message}");
            }
            return (uid, null);
        }
    }
}
