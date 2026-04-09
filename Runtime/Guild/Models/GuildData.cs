using Firebase.Firestore;

namespace Suhdo.FSM.Team.Models
{
    [FirestoreData]
    public class GuildData
    {
        // Field ảo trên client để giữ Reference ID
        public string GuildId { get; set; }

        [FirestoreProperty("name")]
        public string Name { get; set; }

        [FirestoreProperty("description")]
        public string Description { get; set; }

        [FirestoreProperty("leaderId")]
        public string LeaderId { get; set; }

        [FirestoreProperty("memberCount")]
        public int MemberCount { get; set; }

        [FirestoreProperty("joinType")]
        public string JoinType { get; set; } // "open" | "invite_only"

        [FirestoreProperty("region")]
        public string Region { get; set; }

        [FirestoreProperty("createdAt")]
        public object CreatedAt { get; set; }
    }
}
