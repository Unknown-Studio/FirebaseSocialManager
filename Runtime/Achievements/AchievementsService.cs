using System;
using System.Threading;
using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Firestore;
using UnityEngine;

namespace SocialManager.Achievements
{
    public class AchievementsService<T> : IAchievementsService<T> where T : class, new()
    {
        private const string COLLECTION_USERS = "users";
        private const string SUBCOLLECTION_DATA = "data";
        private const string DOC_ACHIEVEMENTS = "achievements";

        private readonly FirebaseFirestore _db;
        private readonly FirebaseAuth _auth;

        public AchievementsService(FirebaseFirestore firestore, FirebaseAuth auth)
        {
            _db = firestore;
            _auth = auth;
        }

        private string CurrentUserId => _auth.CurrentUser?.UserId;

        public async Task<T> FetchAchievementsAsync(string userId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(userId)) return null;

            try
            {
                DocumentReference docRef = _db.Collection(COLLECTION_USERS).Document(userId)
                    .Collection(SUBCOLLECTION_DATA).Document(DOC_ACHIEVEMENTS);
                
                DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();
                
                if (snapshot.Exists)
                {
                    return snapshot.ConvertTo<T>();
                }
                
                return new T(); // Trả về giá trị mặc định nếu chưa có record
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AchievementsService] Lỗi khi tải achievements: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> UpdateAchievementsAsync(T achievements, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(CurrentUserId))
            {
                Debug.LogError("[AchievementsService] Lỗi: User chưa đăng nhập.");
                return false;
            }

            try
            {
                DocumentReference docRef = _db.Collection(COLLECTION_USERS).Document(CurrentUserId)
                    .Collection(SUBCOLLECTION_DATA).Document(DOC_ACHIEVEMENTS);

                // Dùng phương pháp SetOptions.MergeAll để hỗ trợ update cục bộ hoặc chèn thêm
                await docRef.SetAsync(achievements, SetOptions.MergeAll);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AchievementsService] Lỗi khi update Achievements: {ex.Message}");
                return false;
            }
        }
    }
}
