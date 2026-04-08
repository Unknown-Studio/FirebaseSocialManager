using Firebase.Firestore;

namespace SocialManager.Team.Models
{
    [FirestoreData]
    public class GuildMessage
    {
        // Field ảo trên client
        public string MessageId { get; set; }

        [FirestoreProperty("senderId")]
        public string SenderId { get; set; }

        [FirestoreProperty("senderName")]
        public string SenderName { get; set; }

        [FirestoreProperty("text")]
        public string Text { get; set; }

        [FirestoreProperty("timestamp")]
        public object Timestamp { get; set; }
    }
}
