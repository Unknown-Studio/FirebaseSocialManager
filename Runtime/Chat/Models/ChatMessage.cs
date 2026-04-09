using Firebase.Firestore;

namespace Suhdo.FSM.Chat.Models
{
    [FirestoreData]
    public class ChatMessage
    {
        public string MessageId { get; set; } // Field ảo 

        [FirestoreProperty("senderId")]
        public string SenderId { get; set; }

        [FirestoreProperty("text")]
        public string Text { get; set; }

        [FirestoreProperty("timestamp")]
        public object Timestamp { get; set; }
    }
}
