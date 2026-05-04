using System;
using System.Collections.Generic;
using System.Linq;
using Firebase.Auth;
using Suhdo.FSM.Chat;
using Suhdo.FSM.Chat.Models;
using Suhdo.FSM.Friends;
using UnityEngine;

namespace Suhdo.FSM.Social
{
    public class SocialNotificationManager : IDisposable
    {
        private readonly IChatService _chatService;
        private readonly IFriendService _friendService;
        private readonly FirebaseAuth _auth;

        private IDisposable _chatListener;
        private IDisposable _friendListener;

        public int ChatUnreadCount { get; private set; }
        public int FriendRequestCount { get; private set; }
        public int TotalUnreadCount => ChatUnreadCount + FriendRequestCount;

        public event Action OnNotificationChanged;

        public SocialNotificationManager(IChatService chatService, IFriendService friendService, FirebaseAuth auth)
        {
            _chatService = chatService;
            _friendService = friendService;
            _auth = auth;

            // Nếu đã đăng nhập thì bắt đầu lắng nghe ngay
            if (_auth.CurrentUser != null)
            {
                StartListening();
            }

            // Theo dõi trạng thái đăng nhập để bật/tắt listener
            _auth.StateChanged += HandleAuthStateChanged;
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

            string currentUserId = _auth.CurrentUser?.UserId;
            if (string.IsNullOrEmpty(currentUserId)) return;

            // 1. Lắng nghe tin nhắn chat
            _chatListener = _chatService.ListenForMyRooms(rooms =>
            {
                ChatUnreadCount = 0;
                foreach (var room in rooms)
                {
                    if (room.UnreadCount != null && room.UnreadCount.TryGetValue(currentUserId, out int count))
                    {
                        ChatUnreadCount += count;
                    }
                }
                NotifyChanged();
            });

            // 2. Lắng nghe lời mời kết bạn
            _friendListener = _friendService.ListenForFriendRequests(count =>
            {
                FriendRequestCount = count;
                NotifyChanged();
            });
        }

        public void StopListening()
        {
            _chatListener?.Dispose();
            _chatListener = null;

            _friendListener?.Dispose();
            _friendListener = null;
        }

        private void ResetCounts()
        {
            ChatUnreadCount = 0;
            FriendRequestCount = 0;
            NotifyChanged();
        }

        private void NotifyChanged()
        {
            OnNotificationChanged?.Invoke();
        }

        public void Dispose()
        {
            _auth.StateChanged -= HandleAuthStateChanged;
            StopListening();
        }
    }
}
