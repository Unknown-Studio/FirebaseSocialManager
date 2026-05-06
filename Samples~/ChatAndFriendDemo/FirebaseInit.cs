using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using Firebase.Database;
using Suhdo.FSM.Chat;
using Suhdo.FSM.Friends;
using Suhdo.FSM.Friends.Models;
using Suhdo.FSM.Presence;
using Suhdo.FSM.Profile;
using Suhdo.FSM.Profile.Models;
using UnityEngine;

namespace Suhdo.FSM.Sample.FriendChat
{
    public class FirebaseInit : MonoBehaviour
    {
        public FirebaseApp app;
        
        // Sử dụng interface cơ sở để giữ tính tương thích, 
        // hoặc dùng kiểu cụ thể nếu bạn chỉ có 1 loại Profile trong game.
        public static IProfileService<MyGameProfile> ProfileService;
        
        public static IFriendService<FriendRecord> FriendService;
        public static IChatService ChatService;
        public static IPresenceService PresenceService;

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
                    
                    // Khởi tạo với kiểu Profile tùy chỉnh: MyGameProfile
                    ProfileService = new ProfileService<MyGameProfile>(db, auth);
                    FriendService = new FriendService<FriendRecord>(db, auth);
                    ChatService = new ChatService(db, auth);
                    PresenceService = new PresenceService(FirebaseDatabase.DefaultInstance, auth);

                    // Cập nhật thông số tùy chỉnh thông qua lambda linh hoạt
                    await ProfileService.UpdateMyProfileAsync(p => {
                        p.DisplayName = "PewPew";
                        p.AvatarId = "0";
                        p.FrameId = "0";
                        p.Level = 99; // Trường tùy chỉnh
                        p.Score = 123456; // Trường tùy chỉnh
                    });
                    
                    await PresenceService.SetOnlineAsync();
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