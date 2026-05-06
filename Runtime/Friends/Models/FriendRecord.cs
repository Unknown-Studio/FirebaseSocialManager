using Firebase.Firestore;

namespace Suhdo.FSM.Friends.Models
{
    [FirestoreData]
    public class FriendRecord
    {
        // DocumentID đại diện cho targetUserId nên không lưu trực tiếp thông số ID vào payload Json
        public string Uid { get; set; }

        [FirestoreProperty("status")]
        public string Status { get; set; } // "pending_sent", "pending_received", "accepted"

        [FirestoreProperty("updatedAt")]
        public object UpdatedAt { get; set; } // Sử dụng object để handle dạng FieldValue.ServerTimestamp hoặc Timestamp cast
    }
}
