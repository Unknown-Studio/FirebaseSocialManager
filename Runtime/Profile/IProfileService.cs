using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Suhdo.FSM.Profile.Models;

namespace Suhdo.FSM.Profile
{
    public interface IProfileService<TProfile> where TProfile : UserProfile, new()
    {
        /// <summary>
        /// Cập nhật profile của bản thân một cách linh hoạt. 
        /// Nếu chưa có profile, hệ thống sẽ tự động tạo mới.
        /// </summary>
        Task<bool> UpdateMyProfileAsync(Action<TProfile> updateAction, CancellationToken cancellationToken = default);

        /// <summary>
        /// Kéo dữ liệu của Bản thân
        /// </summary>
        Task<TProfile> FetchMyProfileAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy hồ sơ công khai của một người chơi bất kỳ (Dùng cho Leaderboard/Bang hội)
        /// </summary>
        Task<TProfile> FetchPublicProfileAsync(string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Tra cứu UserProfile thông qua mã Friend Code ngắn (Dùng để tìm bạn)
        /// </summary>
        Task<TProfile> FindProfileByFriendCodeAsync(string friendCode, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy hàng loạt hồ sơ công khai (Dùng cho chiến lược Lazy Load danh sách bạn bè)
        /// </summary>
        Task<Dictionary<string, TProfile>> FetchPublicProfilesAsync(IEnumerable<string> userIds, CancellationToken cancellationToken = default);
    }
}
