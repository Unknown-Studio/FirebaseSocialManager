using System.Threading;
using System.Threading.Tasks;
using Suhdo.FSM.Profile.Models;

namespace Suhdo.FSM.Profile
{
    public interface IProfileService
    {
        /// <summary>
        /// Update Profile nếu đã tồn tại hoặc tạo lần đầu tiên đăng nhập
        /// </summary>
        Task<bool> InitializeOrUpdateProfileAsync(string displayName, string avatarId, string frameId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Kéo dữ liệu của Bản thân
        /// </summary>
        Task<UserProfile> FetchMyProfileAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy hồ sơ công khai của một người chơi bất kỳ (Dùng cho Leaderboard/Bang hội)
        /// </summary>
        Task<UserProfile> FetchPublicProfileAsync(string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Tra cứu UserProfile thông qua mã Friend Code ngắn (Dùng để tìm bạn)
        /// </summary>
        Task<UserProfile> FindProfileByFriendCodeAsync(string friendCode, CancellationToken cancellationToken = default);


    }
}
