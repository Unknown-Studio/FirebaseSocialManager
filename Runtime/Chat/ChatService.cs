using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Firestore;
using Suhdo.FSM.Chat.Models;
using UnityEngine;

namespace Suhdo.FSM.Chat
{
    public class ChatService : IChatService
    {
        private const string COLLECTION_CHATS = "private_chats";
        
        private readonly FirebaseFirestore _db;
        private readonly FirebaseAuth _auth;

        public ChatService(FirebaseFirestore firestore, FirebaseAuth auth)
        {
            _db = firestore;
            _auth = auth;
        }

        private string CurrentUserId => _auth.CurrentUser?.UserId;

        // Mã hóa ID của cuộc phiếm đàm dựa vào 2 UID theo thứ tự Alphabet
        public string GetChatRoomId(string userA, string userB)
        {
            if (string.IsNullOrEmpty(userA) || string.IsNullOrEmpty(userB)) return string.Empty;
            int compare = string.Compare(userA, userB, StringComparison.Ordinal);
            return compare < 0 ? $"{userA}_{userB}" : $"{userB}_{userA}";
        }

        public async Task<PrivateChatRoom> GetRoomInfoAsync(string roomId, CancellationToken cancellationToken = default)
        {
            try
            {
                var doc = await _db.Collection(COLLECTION_CHATS).Document(roomId).GetSnapshotAsync();
                if (!doc.Exists) return null;
                
                var room = doc.ConvertTo<PrivateChatRoom>();
                room.ChatId = doc.Id;
                return room;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ChatService] Lỗi GetRoomInfoAsync: {ex.Message}");
                return null;
            }
        }

        public async Task<List<PrivateChatRoom>> FetchAllMyChatRoomsAsync(CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(CurrentUserId)) return new List<PrivateChatRoom>();

            try
            {
                // Gọi API lấy ra tất cả document thuộc Collection PrivateChats có chứa UID bằng hàm WhereArrayContains
                QuerySnapshot snapshot = await _db.Collection(COLLECTION_CHATS)
                     .WhereArrayContains("participants", CurrentUserId)
                     .GetSnapshotAsync();
                
                List<PrivateChatRoom> rooms = new List<PrivateChatRoom>();
                foreach (DocumentSnapshot doc in snapshot.Documents)
                {
                    var r = doc.ConvertTo<PrivateChatRoom>();
                    r.ChatId = doc.Id;
                    rooms.Add(r);
                }
                return rooms;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ChatService] Lỗi FetchAllMyChatRoomsAsync: {ex.Message}");
                return new List<PrivateChatRoom>();
            }
        }

        public async Task<List<ChatMessage>> GetMessagesHistoryAsync(string roomId, int limit = 50, CancellationToken cancellationToken = default)
        {
            try
            {
                // Lấy `limit` dòng tin nhắn xếp từ mới nhất xuống (do descending)
                QuerySnapshot snapshot = await _db.Collection(COLLECTION_CHATS).Document(roomId).Collection("messages")
                     .OrderByDescending("timestamp")
                     .Limit(limit)
                     .GetSnapshotAsync();
                
                List<ChatMessage> msgs = new List<ChatMessage>();
                foreach (DocumentSnapshot doc in snapshot.Documents)
                {
                    var msg = doc.ConvertTo<ChatMessage>();
                    msg.MessageId = doc.Id;
                    msgs.Add(msg);
                }

                // Chuyển Data mảng từ mới -> cũ thành list Cũ -> Mới để tương thích với UI giao diện Game (Scroll flow)
                msgs.Reverse();
                return msgs;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ChatService] Lỗi GetMessagesHistoryAsync: {ex.Message}");
                return new List<ChatMessage>();
            }
        }

        public async Task<bool> SendMessageAsync(string roomId, string targetId, string text, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(CurrentUserId)) return false;

            try
            {
                WriteBatch batch = _db.StartBatch();
                DocumentReference roomRef = _db.Collection(COLLECTION_CHATS).Document(roomId);
                // Tạo một reference rỗng để mựới lấy mã PushID ngẫu nhiên từ Firebase Database
                DocumentReference msgRef = roomRef.Collection("messages").Document(); 
                
                var msgData = new ChatMessage
                {
                    SenderId = CurrentUserId,
                    Text = text,
                    Timestamp = FieldValue.ServerTimestamp
                };
                
                // 1. Tạo tin nhắn mới trong room
                batch.Set(msgRef, msgData);

                // 2. Chỉnh sửa "LastMessage" trên Root Room và Tăng số lượng tin chưa đọc bằng FieldValue.Increment
                // Dùng Dict lồng nhau vì lệnh SetOptions.MergeAll yêu cầu mapping chuẩn phân tầng thay vì string "a.b" 
                var unreadDict = new Dictionary<string, object> { { targetId, FieldValue.Increment(1) } };
                var roomUpdates = new Dictionary<string, object>
                {
                    { "lastMessage", text },
                    { "lastMessageTime", FieldValue.ServerTimestamp },
                    { "participants", new List<string> { CurrentUserId, targetId } }, // Đề phòng phòng mới tinh 100% chưa có array Participants
                    { "unreadCount", unreadDict } // Chỉ tăng Unread của đối phương
                };
                
                // Dùng phương pháp MergeAll để đảm bảo RoomRoot được override đúng số mà không bay màu các field ẩn
                batch.Set(roomRef, roomUpdates, SetOptions.MergeAll);

                await batch.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ChatService] Lỗi Gửi Tin Nhắn Batch Chat: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> MarkAsReadAsync(string roomId, CancellationToken cancellationToken = default)
        {
            try
            {
                DocumentReference roomRef = _db.Collection(COLLECTION_CHATS).Document(roomId);
                // Với UpdateAsync, ta được phép dùng FieldPath gốc của Firebase truyền nguyên khối làm Key
                var roomUpdates = new Dictionary<FieldPath, object>
                {
                    { new FieldPath("unreadCount", CurrentUserId), 0 }
                };
                await roomRef.UpdateAsync(roomUpdates);
                return true;
            }
            catch (Exception ex)
            {
                 Debug.LogError($"[ChatService] Lỗi MarkAsRead: {ex.Message}");
                 return false;
            }
        }

        public IDisposable ListenForNewMessages(string roomId, Action<ChatMessage> onMessageAdded)
        {
            // Thiết lập đầu thu dính vào Snapshot Listener với Limit chặn trên cùng để tiết kiệm read
            Query query = _db.Collection(COLLECTION_CHATS).Document(roomId).Collection("messages")
                .OrderByDescending("timestamp").Limit(1);

            // Firebase C# SDK dùng Action<QuerySnapshot> gốc thay vì mô hình Event (sender, args)
            Action<QuerySnapshot> listener = (snapshot) =>
            {
                if (snapshot == null) return;

                foreach (DocumentChange change in snapshot.GetChanges())
                {
                    if (change.ChangeType == DocumentChange.Type.Added)
                    {
                        var msg = change.Document.ConvertTo<ChatMessage>();
                        msg.MessageId = change.Document.Id;
                        
                        // Kích hoạt Data qua hệ callback cho script UI hiển thị ngầm
                        onMessageAdded?.Invoke(msg);
                    }
                }
            };

            var listenerRegistration = query.Listen(listener);

            return new ListenerDisposer(listenerRegistration);
        }

        // Tự động giải phóng GC Wrapper để Cấu trúc UI của Unity tự gọi dọn rác .Dispose() an toàn khi người dùng thoát khỏi Screen Chat
        private class ListenerDisposer : IDisposable
        {
            private ListenerRegistration _registration;
            public ListenerDisposer(ListenerRegistration registration) => _registration = registration;
            public void Dispose()
            {
                _registration?.Stop();
                _registration = null;
            }
        }
    }
}
