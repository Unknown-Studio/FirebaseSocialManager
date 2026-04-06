using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Firebase.Auth;
using Firebase.Firestore;
using UnityEngine;

namespace SocialManager.SaveGame
{
    public class SaveGameService : ISaveGameService
    {
        private const string COLLECTION_USERS = "users";
        private const string COLLECTION_SAVE = "save_data";
        private const string DOCUMENT_CURRENT = "current";

        private readonly FirebaseFirestore _db;
        private readonly FirebaseAuth _auth;

        public SaveGameService(FirebaseFirestore firestore, FirebaseAuth auth)
        {
            _db = firestore;
            _auth = auth;
        }

        private string CurrentUserId => _auth.CurrentUser?.UserId;

        private DocumentReference GetSaveDocRef()
        {
            if (string.IsNullOrEmpty(CurrentUserId)) return null;
            return _db.Collection(COLLECTION_USERS).Document(CurrentUserId).Collection(COLLECTION_SAVE).Document(DOCUMENT_CURRENT);
        }

        public async UniTask<bool> SaveAsync<T>(T data, CancellationToken cancellationToken = default)
        {
            var docRef = GetSaveDocRef();
            if (docRef == null)
            {
                Debug.LogError("[SaveGameService] Không tìm thấy Auth UID để lưu game.");
                return false;
            }

            try
            {
                // Sử dụng SetAsync để Ghi đè toàn bộ (mặc định cho Save Snapshot)
                await docRef.SetAsync(data).AsUniTask();
                Debug.Log($"[SaveGameService] Đã lưu dữ liệu ({typeof(T).Name}) lên Cloud thành công cho UID: {CurrentUserId}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveGameService] Lỗi khi lưu game ({typeof(T).Name}): {ex.Message}");
                return false;
            }
        }

        public async UniTask<T> LoadAsync<T>(CancellationToken cancellationToken = default) where T : class
        {
            var docRef = GetSaveDocRef();
            if (docRef == null) return null;

            try
            {
                DocumentSnapshot snapshot = await docRef.GetSnapshotAsync().AsUniTask();
                if (snapshot.Exists)
                {
                    return snapshot.ConvertTo<T>();
                }
                
                Debug.Log($"[SaveGameService] Chưa có dữ liệu lưu ({typeof(T).Name}) nào trên Cloud.");
                return null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveGameService] Lỗi khi tải game ({typeof(T).Name}): {ex.Message}");
                return null;
            }
        }

        public async UniTask<bool> DeleteSaveAsync(CancellationToken cancellationToken = default)
        {
            var docRef = GetSaveDocRef();
            if (docRef == null) return false;

            try
            {
                await docRef.DeleteAsync().AsUniTask();
                Debug.Log("[SaveGameService] Đã xóa dữ liệu lưu thành công.");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveGameService] Lỗi khi xóa save: {ex.Message}");
                return false;
            }
        }
    }
}
