using Firebase.Firestore;

namespace SocialManager.Team.Models
{
    [FirestoreData]
    public class GuildMember
    {
        // Field ảo trên client (sẽ được map với tham chiếu Document Id chính là userId)
        public string UserId { get; set; }

        [FirestoreProperty("role")]
        public string Role { get; set; } // "leader" | "co-leader" | "member"

        [FirestoreProperty("joinedAt")]
        public object JoinedAt { get; set; }
    }
}
