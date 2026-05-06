using Firebase.Firestore;

namespace Suhdo.FSM.Profile.Models
{
    [FirestoreData]
    public class UserProfile
    {
        // Field ảo trên code để chứa tham chiếu (Lấy từ DocumentID của db, không lưu đè)
        public string Uid { get; set; }

        [FirestoreProperty("displayName")]
        public string DisplayName { get; set; }

        // Cấu trúc chuỗi số kết bạn ngắn gọn 6 chữ cái (e.g. "AB1234") thay cho UID gốc dài lố
        [FirestoreProperty("friendCode")]
        public string FriendCode { get; set; }

        [FirestoreProperty("avatarId")]
        public string AvatarId { get; set; }

        [FirestoreProperty("frameId")]
        public string FrameId { get; set; }

        [FirestoreProperty("lastLogin")]
        public object LastLogin { get; set; }

        [FirestoreProperty("createdAt")]
        public object ServerCreatedAt { get; set; }
        
        // Lưu ý: Các trường như Level, Score, v.v. nên được định nghĩa ở class kế thừa 
        // trong từng project cụ thể để đảm bảo tính linh hoạt.
    }
}
