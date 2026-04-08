using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Firestore;
using SocialManager.Profile.Models;
using UnityEngine;
using UserProfile = SocialManager.Profile.Models.UserProfile;

namespace SocialManager.Profile
{
    public class ProfileService : IProfileService
    {
        private const string COLLECTION_USERS = "users";
        
        private readonly FirebaseFirestore _db;
        private readonly FirebaseAuth _auth;

        private UserProfile _cachedMyProfile;

        // Dependency Injection Setup: Nhận instance truyền vào
        public ProfileService(FirebaseFirestore firestore, FirebaseAuth auth)
        {
            _db = firestore;
            _auth = auth;
        }

        private string CurrentUserId => _auth.CurrentUser?.UserId;

        public async UniTask<bool> InitializeOrUpdateProfileAsync(string displayName, string avatarId, string frameId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(CurrentUserId))
            {
                Debug.LogError("[ProfileService] Lỗi: Người chơi chưa đăng nhập Firebase Auth.");
                return false;
            }

            try
            {
                DocumentReference userDoc = _db.Collection(COLLECTION_USERS).Document(CurrentUserId);
                
                // GetSnapshotAsync của Firebase trả về System.Threading.Tasks.Task, chuyển nó sang struct UniTask
                DocumentSnapshot snapshot = await userDoc.GetSnapshotAsync().AsUniTask();
                
                if (snapshot.Exists)
                {
                    // Cập nhật record (Bảo mật: client không thể gửi trường Level, TotalScore được nhờ Rule)
                    var updates = new Dictionary<string, object>
                    {
                        { "displayName", displayName },
                        { "avatarId", avatarId },
                        { "frameId", frameId },
                        { "lastLogin", FieldValue.ServerTimestamp }
                    };
                    
                    // Tự động gán mã FriendCode nếu profile cũ chưa từng được sinh mã
                    var oldProfile = snapshot.ConvertTo<UserProfile>();
                    if (string.IsNullOrEmpty(oldProfile.FriendCode))
                    {
                        updates.Add("friendCode", await GenerateUniqueFriendCodeAsync(cancellationToken));
                    }
                    
                    await userDoc.UpdateAsync(updates).AsUniTask();
                }
                else
                {
                    // Dữ liệu tạo mới dựa hoàn toàn vào Model (Type safe) thay vì Dictionary json
                    var newProfile = new UserProfile
                    {
                        DisplayName = displayName,
                        AvatarId = avatarId,
                        FrameId = frameId,
                        FriendCode = await GenerateUniqueFriendCodeAsync(cancellationToken), // Khởi tạo ngẫu nhiên có Validate Check Duplicate 
                        Level = 1,
                        GuildId = "",
                        ServerCreatedAt = FieldValue.ServerTimestamp,
                        LastLogin = FieldValue.ServerTimestamp
                    };
                    
                    // SetOptions.MergeAll để đảm bảo an toàn ghi đè field
                    await userDoc.SetAsync(newProfile, SetOptions.MergeAll).AsUniTask();
                }

                _cachedMyProfile = null; // Clean cache nếu fetch
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ProfileService] Lỗi khi tạo/update file: {ex.Message}");
                return false;
            }
        }

        public async UniTask<UserProfile> FetchMyProfileAsync(CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(CurrentUserId)) 
            {
                Debug.LogWarning("[ProfileService] Current Auth info missing.");
                return null;
            }

            // Gọi từ local cache để tiết kiệm read operations cho Firebase
            if (_cachedMyProfile != null)
                return _cachedMyProfile;

            UserProfile profileInfo = await FetchPublicProfileAsync(CurrentUserId, cancellationToken);
            
            // Xử lý tự Vá lỗi: Nếu tải về phát hiện User Profile cũ từ đời đầu chưa có Friend Code thì cấp ngay
            if (profileInfo != null && string.IsNullOrEmpty(profileInfo.FriendCode))
            {
                string newFriendCode = await GenerateUniqueFriendCodeAsync(cancellationToken);
                profileInfo.FriendCode = newFriendCode;
                
                DocumentReference userDoc = _db.Collection(COLLECTION_USERS).Document(CurrentUserId);
                await userDoc.UpdateAsync(new Dictionary<string, object> { { "friendCode", newFriendCode } }).AsUniTask();
                
                Debug.Log($"[ProfileService] Đã tự động vá lỗi hệ thống: Tạo bù FriendCode mới [{newFriendCode}] cho tài khoản hệ cũ.");
            }

            _cachedMyProfile = profileInfo;
            return _cachedMyProfile;
        }

        public async UniTask<UserProfile> FetchPublicProfileAsync(string userId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(userId)) return null;

            try
            {
                DocumentReference userDoc = _db.Collection(COLLECTION_USERS).Document(userId);
                DocumentSnapshot snapshot = await userDoc.GetSnapshotAsync().AsUniTask();

                if (snapshot.Exists)
                {
                    // Tự động deserialize toàn bộ trường
                    UserProfile profile = snapshot.ConvertTo<UserProfile>();
                    profile.Uid = snapshot.Id; 
                    return profile;
                }
                
                Debug.LogWarning($"[ProfileService] Không tìm thấy Profile cho UID {userId}");
                return null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ProfileService] Error lấy data {userId}: {ex.Message}");
                return null;
            }
        }

        public async UniTask<UserProfile> FindProfileByFriendCodeAsync(string friendCode, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(friendCode)) return null;

            try
            {
                // Truy vấn NoSQL cực nhanh không cần qua Document ID 
                QuerySnapshot snapshot = await _db.Collection(COLLECTION_USERS)
                    .WhereEqualTo("friendCode", friendCode)
                    .Limit(1)
                    .GetSnapshotAsync().AsUniTask();

                if (snapshot.Count > 0)
                {
                    // SDK C# của Firebase trả IList kiểu gộp, nên phải dùng IEnumerator hoặc FirstOrDefault thay vì Index [0]
                    foreach (var doc in snapshot.Documents)
                    {
                        UserProfile profile = doc.ConvertTo<UserProfile>();
                        profile.Uid = doc.Id;
                        return profile;
                    }
                }
                
                Debug.LogWarning($"[ProfileService] Không tìm thấy ai có FriendCode = {friendCode}");
                return null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ProfileService] Lỗi query tìm Code {friendCode}: {ex.Message}");
                return null;
            }
        }

        private string GenerateRandomShortCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new System.Random();
            var result = new char[6];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = chars[random.Next(chars.Length)];
            }
            return new string(result);
        }

        // Sinh mã code và kết nối Database để đảm bảo mã Code không bao giờ bị trùng (Collision Prevention)
        private async UniTask<string> GenerateUniqueFriendCodeAsync(CancellationToken cancellationToken = default)
        {
            int maxAttempts = 10;
            for (int i = 0; i < maxAttempts; i++)
            {
                string code = GenerateRandomShortCode();
                
                // Quét xem Firestore đã từng có ông nào sử dụng cái FriendCode này hay chưa
                QuerySnapshot snapshot = await _db.Collection(COLLECTION_USERS)
                    .WhereEqualTo("friendCode", code)
                    .Limit(1)
                    .GetSnapshotAsync().AsUniTask();
                
                if (snapshot.Count == 0) 
                {
                    return code; // Code trắng hoàn toàn -> Duyệt!
                }
                Debug.LogWarning($"[ProfileService] FriendCode {code} bị trùng! Đang lấy số mới hên xui...");
            }
            
            // Xui đến mức 10 lần quay lại đụng nhầm 10 người thì kéo dài thêm ID ra chút thay vì crash server
            return GenerateRandomShortCode() + UnityEngine.Random.Range(10, 99).ToString();
        }

    }
}
