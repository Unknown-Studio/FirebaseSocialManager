using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using SocialManager.Chat.Models;

namespace SocialManager.Chat
{
    public interface IChatService
    {
        string GetChatRoomId(string userA, string userB);
        UniTask<PrivateChatRoom> GetRoomInfoAsync(string roomId, CancellationToken cancellationToken = default);
        UniTask<List<PrivateChatRoom>> FetchAllMyChatRoomsAsync(CancellationToken cancellationToken = default);
        UniTask<List<ChatMessage>> GetMessagesHistoryAsync(string roomId, int limit = 50, CancellationToken cancellationToken = default);
        UniTask<bool> SendMessageAsync(string roomId, string targetId, string text, CancellationToken cancellationToken = default);
        UniTask<bool> MarkAsReadAsync(string roomId, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Tạo một cái vòi nước Lắng nghe tin nhắn mới dạng Realtime
        /// </summary>
        IDisposable ListenForNewMessages(string roomId, Action<ChatMessage> onMessageAdded);
    }
}
