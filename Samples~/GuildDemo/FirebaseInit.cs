using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using SocialManager.Profile;
using SocialManager.Team;
using UnityEngine;

namespace SocialManager.Sample.Guild
{
    public class FirebaseInit : MonoBehaviour
    {
        public static IGuildService GuildService;

        private void Awake()
        {
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(async task =>
            {
                if (task.Result == DependencyStatus.Available)
                {
                    Debug.Log("Firebase Initialized!");
                    await AutoLoginAnonymous();
                    var db = FirebaseFirestore.DefaultInstance;
                    var auth = FirebaseAuth.DefaultInstance;
                    GuildService = new GuildService(db, auth);
                }
                else
                {
                    Debug.LogError("Firebase dependencies failed: " + task.Result);
                }
            });
        }

        private async Task AutoLoginAnonymous()
        {
            if (FirebaseAuth.DefaultInstance.CurrentUser != null)
            {
                Debug.Log("Đã đăng nhập rồi. UserId: " + FirebaseAuth.DefaultInstance.CurrentUser.UserId);
                return;
            }

            await FirebaseAuth.DefaultInstance.SignInAnonymouslyAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogError("Đăng nhập Anonymous thất bại: " + task.Exception);
                }
                else
                {
                    Debug.Log("=== ĐĂNG NHẬP ANONYMOUS THÀNH CÔNG ===");
                    Debug.Log("UserId: " + FirebaseAuth.DefaultInstance.CurrentUser.UserId);

                    // Lưu UserId để dùng sau này (optional)
                    PlayerPrefs.SetString("UserId", FirebaseAuth.DefaultInstance.CurrentUser.UserId);
                }
            });
        }
    }
}