using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Suhdo.FSM.Chat.Models;

namespace Suhdo.FSM.Chat
{
    public interface IChatService
    {
        string GetChatRoomId(string userA, string userB);
        Task<PrivateChatRoom> GetRoomInfoAsync(string roomId, CancellationToken cancellationToken = default);
        Task<List<PrivateChatRoom>> FetchAllMyChatRoomsAsync(CancellationToken cancellationToken = default);
        Task<List<ChatMessage>> GetMessagesHistoryAsync(string roomId, int limit = 50, CancellationToken cancellationToken = default);
        Task<bool> SendMessageAsync(string roomId, string targetId, string text, CancellationToken cancellationToken = default);
        Task<bool> MarkAsReadAsync(string roomId, CancellationToken cancellationToken = default);
        Task<string> CreateRoomAsync(string targetUserId, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Tạo một cái vòi nước Lắng nghe tin nhắn mới dạng Realtime
        /// </summary>
        IDisposable ListenForNewMessages(string roomId, Action<ChatMessage> onMessageAdded);
    }
}
