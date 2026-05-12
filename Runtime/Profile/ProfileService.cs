using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Firestore;
using Suhdo.FSM.Core;
using UnityEngine;
using UserProfile = Suhdo.FSM.Profile.Models.UserProfile;

namespace Suhdo.FSM.Profile
{
    public class ProfileService<TProfile> : IProfileService<TProfile> where TProfile : UserProfile, new()
    {
        private const string COLLECTION_USERS = "users";
        
        private readonly FirebaseFirestore _db;
        private readonly FirebaseAuth _auth;

        private TProfile _cachedMyProfile;
        private readonly Dictionary<string, DataCache<TProfile>> _publicProfilesCache = new();
        private readonly TimeSpan _profileCacheDuration;

        public ProfileService(FirebaseFirestore firestore, FirebaseAuth auth, TimeSpan? cacheDuration = null)
        {
            _db = firestore;
            _auth = auth;
            _profileCacheDuration = cacheDuration ?? TimeSpan.FromMinutes(1);
        }

        private string CurrentUserId => _auth.CurrentUser?.UserId;

        public async Task<bool> UpdateMyProfileAsync(Action<TProfile> updateAction, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(CurrentUserId))
            {
                Debug.LogError("[ProfileService] Lỗi: Người chơi chưa đăng nhập Firebase Auth.");
                return false;
            }

            Debug.Log($"[ProfileService] Bắt đầu cập nhật profile cho User: {CurrentUserId}");

            try
            {
                DocumentReference userDoc = _db.Collection(COLLECTION_USERS).Document(CurrentUserId);
                
                Debug.Log("[ProfileService] Đang tải snapshot từ Firestore...");
                DocumentSnapshot snapshot = await userDoc.GetSnapshotAsync();
                
                TProfile profile;
                bool isNew = false;

                if (snapshot.Exists)
                {
                    Debug.Log("[ProfileService] Tìm thấy document cũ. Đang chuyển đổi dữ liệu...");
                    profile = snapshot.ConvertTo<TProfile>();
                }
                else
                {
                    Debug.Log("[ProfileService] Không tìm thấy document. Khởi tạo profile mới.");
                    profile = new TProfile();
                    profile.ServerCreatedAt = FieldValue.ServerTimestamp;
                    isNew = true;
                }

                // Thực thi logic cập nhật từ phía Client (Project-specific)
                updateAction?.Invoke(profile);
                
                // Luôn cập nhật các trường hệ thống cốt lõi
                profile.LastLogin = FieldValue.ServerTimestamp;
                
                // Đảm bảo có FriendCode
                if (string.IsNullOrEmpty(profile.FriendCode))
                {
                    Debug.Log("[ProfileService] FriendCode trống. Đang tạo FriendCode duy nhất...");
                    profile.FriendCode = await GenerateUniqueFriendCodeAsync(cancellationToken);
                    Debug.Log($"[ProfileService] Đã tạo FriendCode mới: {profile.FriendCode}");
                }

                if (isNew)
                {
                    Debug.Log("[ProfileService] Đang thực hiện SetAsync cho document mới...");
                    await userDoc.SetAsync(profile);
                }
                else
                {
                    Debug.Log("[ProfileService] Đang thực hiện SetAsync với MergeAll cho document cũ...");
                    await userDoc.SetAsync(profile, SetOptions.MergeAll);
                }

                Debug.Log("[ProfileService] Cập nhật Profile thành công. Reset cache.");
                _cachedMyProfile = null; // Reset cache bản thân
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ProfileService] Lỗi nghiêm trọng khi cập nhật profile: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }

        public async Task<TProfile> FetchMyProfileAsync(CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(CurrentUserId)) return null;

            if (_cachedMyProfile != null)
                return _cachedMyProfile;

            _cachedMyProfile = await FetchPublicProfileAsync(CurrentUserId, cancellationToken);
            return _cachedMyProfile;
        }

        public async Task<TProfile> FetchPublicProfileAsync(string userId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(userId)) return null;

            if (_publicProfilesCache.TryGetValue(userId, out var cache) && !cache.IsExpired)
            {
                return cache.Data;
            }

            try
            {
                DocumentReference userDoc = _db.Collection(COLLECTION_USERS).Document(userId);
                DocumentSnapshot snapshot = await userDoc.GetSnapshotAsync();

                if (snapshot.Exists)
                {
                    TProfile profile = snapshot.ConvertTo<TProfile>();
                    profile.Uid = snapshot.Id; 
                    
                    if (!_publicProfilesCache.ContainsKey(userId))
                        _publicProfilesCache[userId] = new DataCache<TProfile>(_profileCacheDuration);
                    
                    _publicProfilesCache[userId].Update(profile);
                    
                    return profile;
                }
                return null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ProfileService] Error lấy data {userId}: {ex.Message}");
                return null;
            }
        }

        public async Task<TProfile> FindProfileByFriendCodeAsync(string friendCode, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(friendCode)) return null;

            try
            {
                QuerySnapshot snapshot = await _db.Collection(COLLECTION_USERS)
                    .WhereEqualTo("friendCode", friendCode)
                    .Limit(1)
                    .GetSnapshotAsync();

                if (snapshot.Count > 0)
                {
                    foreach (var doc in snapshot.Documents)
                    {
                        TProfile profile = doc.ConvertTo<TProfile>();
                        profile.Uid = doc.Id;
                        return profile;
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ProfileService] Lỗi query tìm Code {friendCode}: {ex.Message}");
                return null;
            }
        }

        public async Task<Dictionary<string, TProfile>> FetchPublicProfilesAsync(IEnumerable<string> userIds, CancellationToken cancellationToken = default)
        {
            var results = new Dictionary<string, TProfile>();
            var idsToFetch = new List<string>();

            foreach (var id in userIds)
            {
                if (string.IsNullOrEmpty(id)) continue;

                if (_publicProfilesCache.TryGetValue(id, out var cache) && !cache.IsExpired)
                {
                    results[id] = cache.Data;
                }
                else
                {
                    idsToFetch.Add(id);
                }
            }

            if (idsToFetch.Count == 0) return results;

            try
            {
                const int batchSize = 30;
                for (int i = 0; i < idsToFetch.Count; i += batchSize)
                {
                    var currentBatch = idsToFetch.GetRange(i, Math.Min(batchSize, idsToFetch.Count - i));
                    
                    QuerySnapshot snapshot = await _db.Collection(COLLECTION_USERS)
                        .WhereIn(FieldPath.DocumentId, currentBatch)
                        .GetSnapshotAsync();

                    foreach (var doc in snapshot.Documents)
                    {
                        var profile = doc.ConvertTo<TProfile>();
                        profile.Uid = doc.Id;
                        
                        if (!_publicProfilesCache.ContainsKey(doc.Id))
                            _publicProfilesCache[doc.Id] = new DataCache<TProfile>(_profileCacheDuration);
                        
                        _publicProfilesCache[doc.Id].Update(profile);
                        results[doc.Id] = profile;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ProfileService] Lỗi Batch Fetch Profiles: {ex.Message}");
            }

            return results;
        }

        private async Task<string> GenerateUniqueFriendCodeAsync(CancellationToken cancellationToken = default)
        {
            for (int i = 0; i < 10; i++)
            {
                string code = GenerateRandomShortCode();
                QuerySnapshot snapshot = await _db.Collection(COLLECTION_USERS)
                    .WhereEqualTo("friendCode", code)
                    .Limit(1)
                    .GetSnapshotAsync();
                
                if (snapshot.Count == 0) return code;
            }
            return GenerateRandomShortCode() + UnityEngine.Random.Range(10, 99);
        }

        private string GenerateRandomShortCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new System.Random();
            var result = new char[6];
            for (int i = 0; i < result.Length; i++)
                result[i] = chars[random.Next(chars.Length)];
            return new string(result);
        }
    }
}
