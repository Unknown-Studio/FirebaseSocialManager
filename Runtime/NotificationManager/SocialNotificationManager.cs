using System;
using System.Linq;
using System.Threading.Tasks;
using Firebase.Auth;
using Suhdo.FSM.Chat;
using Suhdo.FSM.Friends;

namespace Suhdo.FSM.Social
{
    /// <summary>
    /// Interface cho hệ thống quản lý thông báo xã hội.
    /// Giúp việc Dependency Injection và Unit Test dễ dàng hơn.
    /// </summary>
    public interface ISocialNotificationManager : IDisposable
    {
        int ChatUnreadCount { get; }
        int FriendRequestCount { get; }
        int GuildNotificationCount { get; }
        int TotalNotificationCount { get; }

        event Action OnNotificationChanged;

        void StartListening();
        void StopListening();
        Task FetchInitialCountsAsync();
    }

    /// <summary>
    /// Quản lý tập trung các thông báo từ nhiều nguồn khác nhau (Chat, Friends, Guilds).
    /// Được thiết kế để linh hoạt cho nhiều dự án khác nhau.
    /// </summary>
    public class SocialNotificationManager : ISocialNotificationManager
    {
        private readonly IChatService _chatService;
        private readonly IFriendService _friendService;
        private readonly FirebaseAuth _auth;

        private IDisposable _chatListener;
        private IDisposable _friendListener;
        private IDisposable _guildListener;

        private int _chatUnreadCount;
        private int _friendRequestCount;
        private int _guildNotificationCount;

        public int ChatUnreadCount => _chatUnreadCount;
        public int FriendRequestCount => _friendRequestCount;
        public int GuildNotificationCount => _guildNotificationCount;
        public int TotalNotificationCount => _chatUnreadCount + _friendRequestCount + _guildNotificationCount;

        public event Action OnNotificationChanged;

        /// <summary>
        /// Khởi tạo Manager. Các service có thể là null nếu dự án không sử dụng tính năng đó.
        /// </summary>
        public SocialNotificationManager(
            FirebaseAuth auth,
            IChatService chatService = null,
            IFriendService friendService = null)
        {
            _auth = auth ?? throw new ArgumentNullException(nameof(auth));
            _chatService = chatService;
            _friendService = friendService;

            // Đăng ký theo dõi trạng thái Auth
            _auth.StateChanged += HandleAuthStateChanged;

            // Nếu đã đăng nhập thì bắt đầu ngay
            if (_auth.CurrentUser != null)
            {
                StartListening();
            }
        }

        private void HandleAuthStateChanged(object sender, EventArgs e)
        {
            if (_auth.CurrentUser != null)
            {
                StartListening();
            }
            else
            {
                StopListening();
                ResetCounts();
            }
        }

        public void StartListening()
        {
            StopListening();

            string userId = _auth.CurrentUser?.UserId;
            if (string.IsNullOrEmpty(userId)) return;

            // Lắng nghe Chat nếu service được cung cấp
            if (_chatService != null)
            {
                _chatListener = _chatService.ListenForMyRooms(rooms =>
                {
                    int totalUnread = 0;
                    foreach (var room in rooms)
                    {
                        if (room.UnreadCount != null && room.UnreadCount.TryGetValue(userId, out int count))
                        {
                            totalUnread += count;
                        }
                    }
                    _chatUnreadCount = totalUnread;
                    _chatService.InvalidateCache();
                    NotifyChanged();
                });
            }

            // Lắng nghe Friend Requests nếu service được cung cấp
            if (_friendService != null)
            {
                _friendListener = _friendService.ListenForFriendRequests(count =>
                {
                    _friendRequestCount = count;
                    _friendService.InvalidateCache();
                    NotifyChanged();
                });
            }
            
            // TODO: Lắng nghe Guild Notifications nếu có service tương ứng trong tương lai
        }

        public void StopListening()
        {
            _chatListener?.Dispose();
            _chatListener = null;

            _friendListener?.Dispose();
            _friendListener = null;

            _guildListener?.Dispose();
            _guildListener = null;
        }

        public async Task FetchInitialCountsAsync()
        {
            string userId = _auth.CurrentUser?.UserId;
            if (string.IsNullOrEmpty(userId)) return;

            // 1. Fetch Chat Unread Count
            if (_chatService != null)
            {
                var rooms = await _chatService.FetchAllMyChatRoomsAsync();
                int totalUnread = 0;
                foreach (var room in rooms)
                {
                    if (room.UnreadCount != null && room.UnreadCount.TryGetValue(userId, out int count))
                    {
                        totalUnread += count;
                    }
                }
                _chatUnreadCount = totalUnread;
            }

            // 2. Fetch Friend Request Count
            if (_friendService != null)
            {
                var friends = await _friendService.FetchAllFriendsAsync();
                _friendRequestCount = friends.Count(f => f.Status == "pending_received");
            }

            NotifyChanged();
        }

        private void ResetCounts()
        {
            _chatUnreadCount = 0;
            _friendRequestCount = 0;
            _guildNotificationCount = 0;
            NotifyChanged();
        }

        private void NotifyChanged()
        {
            OnNotificationChanged?.Invoke();
        }

        public void Dispose()
        {
            if (_auth != null)
            {
                _auth.StateChanged -= HandleAuthStateChanged;
            }
            StopListening();
        }
    }
}
