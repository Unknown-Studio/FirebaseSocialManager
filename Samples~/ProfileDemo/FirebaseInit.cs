using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using Suhdo.FSM.Profile;
using Suhdo.FSM.Profile.Models;
using UnityEngine;

namespace Suhdo.FSM.Sample.Profile
{
    public class FirebaseInit : MonoBehaviour
    {
        // Khởi tạo với kiểu DemoProfile để đồng bộ với ProfileTestUI
        public static IProfileService<DemoProfile> ProfileService;

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
                    
                    ProfileService = new ProfileService<DemoProfile>(db, auth);
                    
                    await ProfileService.UpdateMyProfileAsync(p => {
                        p.DisplayName = "Player";
                        p.AvatarId = "0";
                        p.FrameId = "0";
                        p.Level = 1;
                        p.GuildId = "";
                    });
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