using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using SocialManager.Friends.Models;
using SocialManager.Profile.Models;

namespace SocialManager.Friends
{
    public interface IFriendService
    {
        /// <summary>
        /// Lấy toàn bộ danh sách liên quan đến bạn bè
        /// </summary>
        UniTask<List<FriendRecord>> FetchAllFriendsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gửi yêu cầu kết bạn 
        /// (Sử dụng dữ liệu của MyProfile và TargetProfile để gửi File đính kèm lưu phi chuẩn hóa)
        /// </summary>
        UniTask<bool> SendFriendRequestAsync(string targetUserId, UserProfile targetProfile, UserProfile myProfile, CancellationToken cancellationToken = default);

        /// <summary>
        /// Đồng ý hoặc Từ chối yêu cầu kết bạn
        /// </summary>
        UniTask<bool> RespondToFriendRequestAsync(string targetUserId, bool isAccepted, CancellationToken cancellationToken = default);

        /// <summary>
        /// Xóa xóa một người bạn khỏi danh sách
        /// </summary>
        UniTask<bool> RemoveFriendAsync(string targetUserId, CancellationToken cancellationToken = default);
    }
}
