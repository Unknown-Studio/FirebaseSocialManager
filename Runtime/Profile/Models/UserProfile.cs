using Firebase.Firestore;

namespace SocialManager.Profile.Models
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

        [FirestoreProperty("level")]
        public int Level { get; set; } = 1;

        [FirestoreProperty("guildId")]
        public string GuildId { get; set; } = "";

        [FirestoreProperty("achievements")]
        public UserAchievements Achievements { get; set; } = new UserAchievements();

        [FirestoreProperty("lastLogin")]
        public object LastLogin { get; set; }

        [FirestoreProperty("createdAt")]
        public object ServerCreatedAt { get; set; }
    }
}
