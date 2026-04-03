using Firebase.Firestore;

namespace SocialManager.Profile.Models
{
    [FirestoreData]
    public class UserAchievements
    {
        [FirestoreProperty("stagesCleared")]
        public int StagesCleared { get; set; } = 0;

        [FirestoreProperty("highestGlobalRank")]
        public int HighestGlobalRank { get; set; } = 0;

        [FirestoreProperty("heartsSent")]
        public int HeartsSent { get; set; } = 0;

        [FirestoreProperty("totalScore")]
        public long TotalScore { get; set; } = 0;
    }
}
