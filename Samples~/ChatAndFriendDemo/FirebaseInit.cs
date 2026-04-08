using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using SocialManager.Chat;
using SocialManager.Friends;
using SocialManager.Profile;
using UnityEngine;

namespace SocialManager.Sample.FriendChat
{
    public class FirebaseInit : MonoBehaviour
    {
        public FirebaseApp app;
        public static IProfileService ProfileService;
        public static IFriendService FriendService;
        public static IChatService ChatService;

        private void Awake()
        {
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(async task =>
            {
                if (task.Result == DependencyStatus.Available)
                {
                    app = FirebaseApp.DefaultInstance;
                    Debug.Log("Firebase Initialized!");
                    await AutoLoginAnonymous();
                    var db = FirebaseFirestore.DefaultInstance;
                    var auth = FirebaseAuth.DefaultInstance;
                    ProfileService = new ProfileService(db, auth);
                    FriendService = new FriendService(db, auth);
                    ChatService = new ChatService(db, auth);
                    await ProfileService.InitializeOrUpdateProfileAsync("PewPew", "0", "0");
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