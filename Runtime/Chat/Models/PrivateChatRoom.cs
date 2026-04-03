using System.Collections.Generic;
using Firebase.Firestore;

namespace SocialManager.Chat.Models
{
    [FirestoreData]
    public class PrivateChatRoom
    {
        public string ChatId { get; set; } // Field ảo tham chiếu qua ID của collection 1-1

        [FirestoreProperty("participants")]
        public IList<string> Participants { get; set; }

        [FirestoreProperty("lastMessage")]
        public string LastMessage { get; set; }

        [FirestoreProperty("lastMessageTime")]
        public object LastMessageTime { get; set; }

        [FirestoreProperty("unreadCount")]
        public IDictionary<string, int> UnreadCount { get; set; }
    }
}
