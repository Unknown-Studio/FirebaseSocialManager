using System;
using Cysharp.Threading.Tasks;
using Firebase.Auth;
using Firebase.Firestore;
using SocialManager.Profile;
using SocialManager.Profile.Models;
using SocialManager.Achievements;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SocialManager.Sample.Profile
{
    [FirestoreData]
    public class DemoAchievements
    {
        [FirestoreProperty("totalScore")]
        public long TotalScore { get; set; }
        
        [FirestoreProperty("highestGlobalRank")]
        public int HighestGlobalRank { get; set; }
        
        [FirestoreProperty("stagesCleared")]
        public int StagesCleared { get; set; }
    }

    public class ProfileTestUI : MonoBehaviour
    {
        [Header("Profile Input")]
        [SerializeField] private TMP_InputField inputDisplayName;
        [SerializeField] private TMP_InputField inputAvatarId;
        [SerializeField] private TMP_InputField inputFrameId;
        
        [Header("Search Input")]
        [SerializeField] private TMP_InputField inputSearchFriendCode;
        
        [Header("Display")]
        [SerializeField] private TextMeshProUGUI textProfileLog;
        
        [Header("Buttons")]
        [SerializeField] private Button btnUpdateProfile;
        [SerializeField] private Button btnFetchMyProfile;
        [SerializeField] private Button btnSearchByCode;
        [SerializeField] private Button btnTestAchievements;

        // Caching
        private IProfileService ProfileService => FirebaseInit.ProfileService;
        private IAchievementsService<DemoAchievements> AchievementsService { get; set; }
        private string CurrentUserId => FirebaseAuth.DefaultInstance.CurrentUser?.UserId;

        private void Start()
        {
            // Initialize AchievementsService locally with our Generic DemoAchievements struct
            var db = FirebaseFirestore.DefaultInstance;
            var auth = FirebaseAuth.DefaultInstance;
            AchievementsService = new AchievementsService<DemoAchievements>(db, auth);

            btnUpdateProfile.onClick.AddListener(() => UpdateProfileAsync().Forget());
            btnFetchMyProfile.onClick.AddListener(() => FetchMyProfileAsync().Forget());
            btnSearchByCode.onClick.AddListener(() => SearchProfileAsync().Forget());
            btnTestAchievements.onClick.AddListener(() => TestUpdateAchievementsAsync().Forget());
            
            Log("ProfileTestUI Initialized. Please ensure you are logged in via Firebase Auth.");
        }

        private async UniTaskVoid UpdateProfileAsync()
        {
            string name = inputDisplayName.text.Trim();
            string avatar = inputAvatarId.text.Trim();
            string frame = inputFrameId.text.Trim();

            if (string.IsNullOrEmpty(name))
            {
                Log("<color=red>Error: Display Name cannot be empty.</color>");
                return;
            }

            Log($"Updating profile: Name={name}, Avatar={avatar}, Frame={frame}...");
            bool success = await ProfileService.InitializeOrUpdateProfileAsync(name, avatar, frame);
            
            if (success)
            {
                Log("<color=green>Profile updated successfully!</color>");
            }
            else
            {
                Log("<color=red>Failed to update profile. Check console for errors.</color>");
            }
        }

        private async UniTaskVoid FetchMyProfileAsync()
        {
            Log("Fetching your profile & achievements...");
            var profile = await ProfileService.FetchMyProfileAsync();
            var achieves = await AchievementsService.FetchAchievementsAsync(CurrentUserId);
            
            if (profile != null)
            {
                Log("=== YOUR PROFILE ===");
                Log($"ID: {profile.Uid}");
                Log($"Name: {profile.DisplayName}");
                Log($"Friend Code: <color=yellow>{profile.FriendCode}</color>");
                Log($"Level: {profile.Level}");
                Log($"Guild ID: {profile.GuildId}");
            }
            else
            {
                Log("<color=red>Could not fetch your profile. Are you logged in?</color>");
            }

            if (achieves != null)
            {
                Log("=== ACHIEVEMENTS ===");
                Log($"Stages Cleared: {achieves.StagesCleared}");
                Log($"Total Score: {achieves.TotalScore}");
                Log($"Highest Global Rank: {achieves.HighestGlobalRank}");
            }
        }

        private async UniTaskVoid SearchProfileAsync()
        {
            string code = inputSearchFriendCode.text.Trim();
            if (string.IsNullOrEmpty(code))
            {
                Log("Enter a Friend Code to search.");
                return;
            }

            Log($"Searching for Friend Code: {code}...");
            var profile = await ProfileService.FindProfileByFriendCodeAsync(code);
            
            if (profile != null)
            {
                Log($"<color=cyan>User Found!</color>");
                Log($"Name: {profile.DisplayName}");
                Log($"UID: {profile.Uid}");
                Log($"Level: {profile.Level}");
            }
            else
            {
                Log($"<color=red>No user found with code: {code}</color>");
            }
        }

        private async UniTaskVoid TestUpdateAchievementsAsync()
        {
            Log("Setting dummy achievements (Score: 5000, Stages: 30)...");
            var dummyAchieve = new DemoAchievements
            {
                TotalScore = 5000,
                HighestGlobalRank = 2,
                StagesCleared = 30
            };

            bool success = await AchievementsService.UpdateAchievementsAsync(dummyAchieve);
            Log(success ? "<color=green>Achievements updated!</color>" : "<color=red>Update failed.</color>");
        }

        private void Log(string message)
        {
            if (textProfileLog != null)
            {
                textProfileLog.text += $"\n[{DateTime.Now:HH:mm:ss}] {message}";
                Debug.Log(message);
            }
        }
    }
}
