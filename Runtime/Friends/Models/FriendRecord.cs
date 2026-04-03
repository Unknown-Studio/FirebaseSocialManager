using Firebase.Firestore;

namespace SocialManager.Friends.Models
{
    [FirestoreData]
    public class FriendRecord
    {
        // DocumentID đại diện cho targetUserId nên không lưu trực tiếp thông số ID vào payload Json
        public string Uid { get; set; }

        [FirestoreProperty("status")]
        public string Status { get; set; } // "pending_sent", "pending_received", "accepted"

        [FirestoreProperty("friendName")]
        public string FriendName { get; set; }

        [FirestoreProperty("avatarId")]
        public string AvatarId { get; set; }
        
        [FirestoreProperty("frameId")]
        public string FrameId { get; set; }

        [FirestoreProperty("updatedAt")]
        public object UpdatedAt { get; set; } // Sử dụng object để handle dạng FieldValue.ServerTimestamp hoặc Timestamp cast
    }
}
