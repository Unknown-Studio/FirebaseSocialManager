using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SocialManager.Presence.Models;

namespace SocialManager.Presence
{
    public interface IPresenceService
    {
        /// <summary>
        /// Đánh dấu người dùng đang Online và cấu hình OnDisconnect.
        /// </summary>
        Task SetOnlineAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Chủ động đánh dấu người dùng đã Offline (khi logout).
        /// </summary>
        Task SetOfflineAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy trạng thái của danh sách người dùng một lần.
        /// </summary>
        Task<Dictionary<string, UserPresence>> GetStatusesAsync(IEnumerable<string> userIds, CancellationToken cancellationToken = default);
    }
}
