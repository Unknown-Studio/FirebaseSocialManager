using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Suhdo.FSM.Friends.Models;
using Suhdo.FSM.Profile.Models;

namespace Suhdo.FSM.Friends
{
    public interface IFriendService
    {
        /// <summary>
        /// Lấy toàn bộ danh sách liên quan đến bạn bè
        /// </summary>
        Task<List<FriendRecord>> FetchAllFriendsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gửi yêu cầu kết bạn 
        /// (Sử dụng dữ liệu của MyProfile và TargetProfile để gửi File đính kèm lưu phi chuẩn hóa)
        /// </summary>
        Task<bool> SendFriendRequestAsync(string targetUserId, UserProfile targetProfile, UserProfile myProfile, CancellationToken cancellationToken = default);

        /// <summary>
        /// Đồng ý hoặc Từ chối yêu cầu kết bạn
        /// </summary>
        Task<bool> RespondToFriendRequestAsync(string targetUserId, bool isAccepted, CancellationToken cancellationToken = default);

        /// <summary>
        /// Xóa xóa một người bạn khỏi danh sách
        /// </summary>
        Task<bool> RemoveFriendAsync(string targetUserId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Xóa cache để ép buộc tải lại dữ liệu từ Firebase
        /// </summary>
        void InvalidateCache();

        /// <summary>
        /// Lắng nghe số lượng lời mời kết bạn đang chờ (pending_received)
        /// </summary>
        IDisposable ListenForFriendRequests(Action<int> onCountChanged);
    }
}
