using System;
using System.Text;
using Cysharp.Threading.Tasks;
using Firebase.Auth;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SocialManager.UI
{
    public class ChatTestUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_InputField inputTargetFriendCode;
        [SerializeField] private TMP_InputField inputMessageText;
        [SerializeField] private TextMeshProUGUI textLog;
        
        [Header("Buttons")]
        [SerializeField] private Button btnFetchInbox;
        [SerializeField] private Button btnHistory;
        [SerializeField] private Button btnSend;
        [SerializeField] private Button btnMarkRead;
        [SerializeField] private Button btnListen;

        private IDisposable _realtimeListener;
        private string CurrentUserId => FirebaseAuth.DefaultInstance.CurrentUser?.UserId;

        private void Start()
        {
            btnFetchInbox.onClick.AddListener(() => FetchInboxAsync().Forget());
            btnHistory.onClick.AddListener(() => FetchHistoryAsync().Forget());
            btnSend.onClick.AddListener(() => SendMessageAsync().Forget());
            btnMarkRead.onClick.AddListener(() => MarkAsReadAsync().Forget());
            btnListen.onClick.AddListener(ToggleRealtimeListen);
            
            Log("ChatTestUI Đã Kết Nối! Luôn sử dụng FriendCode của tài khoản nhận để test tính năng.");
        }

        private void OnDestroy()
        {
            // Tự động gỡ bộ thu Firestore realtime nếu như bấm Stop Editor tắt Menu
            _realtimeListener?.Dispose();
        }

        // Tái sử dụng logic Giải mã ngược FriendCode thành UID 32 Ký Tự
        private async UniTask<string> ResolveTargetIdAsync()
        {
            string code = inputTargetFriendCode.text.Trim();
            if (string.IsNullOrEmpty(code))
            {
                Log("Vui lòng nhập FriendCode của đối tác muốn chat vào ô đầu tiên!");
                return null;
            }

            var targetProfile = await FirebaseInit.ProfileService.FindProfileByFriendCodeAsync(code);
            if (targetProfile == null)
            {
                Log($"[Lỗi] API báo lại không tìm thấy bất kỳ ai sở hữu FriendCode: {code}");
                return null;
            }
            return targetProfile.Uid;
        }

        private async UniTaskVoid FetchInboxAsync()
        {
            Log("---");
            Log("Đang kết nối Firestore tải Inbox List...");
            var rooms = await FirebaseInit.ChatService.FetchAllMyChatRoomsAsync();
            if (rooms.Count == 0)
            {
                Log("Hộp thư của bạn hiện tại rỗng! Chưa từng chat với ai qua hệ thống mới.");
                return;
            }

            StringBuilder sb = new StringBuilder("=== TẤT CẢ PHÒNG CHAT CỦA BẠN ===\n");
            foreach (var r in rooms)
            {
                string unreadText = "";
                // Tra số lượng từ hệ List Unread Dict (nhớ lấy từ key là UID của chính chình)
                if (r.UnreadCount != null && r.UnreadCount.TryGetValue(CurrentUserId, out int count) && count > 0)
                {
                    unreadText = $" <color=red>[{count} TIN CHƯA ĐỌC]</color>";
                }
                sb.AppendLine($"- Mã phòng: {r.ChatId}");
                sb.AppendLine($"  Gần nhất: {r.LastMessage}{unreadText}");
            }
            Log(sb.ToString());
        }

        private async UniTaskVoid FetchHistoryAsync()
        {
            string targetId = await ResolveTargetIdAsync();
            if (targetId == null) return;

            string roomId = FirebaseInit.ChatService.GetChatRoomId(CurrentUserId, targetId);
            Log("---");
            Log($"[Loading] Tải 50 luồng tin mới nhất tại mã khoang `{roomId}`...");

            var msgs = await FirebaseInit.ChatService.GetMessagesHistoryAsync(roomId, 50);
            
            StringBuilder sb = new StringBuilder($"=== LỊCH SỬ NHẮN TIN GẦN ĐÂY ===\n");
            foreach (var m in msgs)
            {
                string senderName = (m.SenderId == CurrentUserId) ? "<color=green>Me</color>" : "<color=yellow>Partner</color>";
                sb.AppendLine($"[{senderName}]: {m.Text}");
            }
            Log(sb.ToString());
        }

        private async UniTaskVoid SendMessageAsync()
        {
            string text = inputMessageText.text.Trim();
            if (string.IsNullOrEmpty(text))
            {
                Log("Hãy điền vào ô văn bản tin nhắn trước khi nhấn Gửi!");
                return;
            }

            string targetId = await ResolveTargetIdAsync();
            if (targetId == null) return;
            
            string roomId = FirebaseInit.ChatService.GetChatRoomId(CurrentUserId, targetId);

            Log($"Đóng gói Data: '{text}' tới {targetId}...");
            bool isSuccess = await FirebaseInit.ChatService.SendMessageAsync(roomId, targetId, text);
            if (isSuccess)
            {
                Log("-> OK! Thư đã được Batch Write đi vào Database Firebase! (Số đếm Unread của người kia bị +1)");
                inputMessageText.text = ""; // Auto xoá ô cho tiện
            }
            else
            {
                Log("-> LỖI GIAO DỊCH, xem Console màu đỏ trong trình duyệt Unity.");
            }
        }

        private async UniTaskVoid MarkAsReadAsync()
        {
            string targetId = await ResolveTargetIdAsync();
            if (targetId == null) return;
            string roomId = FirebaseInit.ChatService.GetChatRoomId(CurrentUserId, targetId);

            Log("Bắn tín hiệu làm rỗng Unread Count bản thân...");
            bool isSuccess = await FirebaseInit.ChatService.MarkAsReadAsync(roomId);
            Log(isSuccess ? "-> Tẩy trắng chữ thành công! Hệ thống xem như bạn đã Seen cuộc trò chuyện." : "-> FAILED! Call Firebase thất bạt!");
        }

        private async void ToggleRealtimeListen()
        {
            if (_realtimeListener != null)
            {
                // Ngắt mỏ neo kết nối
                _realtimeListener.Dispose();
                _realtimeListener = null;
                btnListen.GetComponentInChildren<TextMeshProUGUI>().text = "Bật Thu Thập Tin Real-time Reng Reng";
                Log("[Ngắt Reng Reng] Đã ngắt Listener Firestore với phòng trên.");
                return;
            }

            string targetId = await ResolveTargetIdAsync();
            if (targetId == null) return;
            string roomId = FirebaseInit.ChatService.GetChatRoomId(CurrentUserId, targetId);

            Log($"[Reng Reng] Đã treo Listener chực chờ tin từ phòng {roomId}. Nếu Cửa sổ Client 2 búng vào nhắn, màn hình Client này lập tức sẽ nổ Text đỏ bự bên dưới!");
            
            _realtimeListener = FirebaseInit.ChatService.ListenForNewMessages(roomId, (newMsg) =>
            {
                // Bộ thu Firebase giật rất lẹ, ngay cả tự bản thân người viết chọt data vô nó cũng bị nảy callback.
                // Ở đây ta phớt lờ mình đi, chỉ in log cho Tin của Client bên kia dội vô
                if (newMsg.SenderId != CurrentUserId) 
                {
                    Log($"<color=orange>[RENG RENG - KHÁCH GỎI ĐẾN]: {newMsg.Text}</color>");
                }
            });
            btnListen.GetComponentInChildren<TextMeshProUGUI>().text = "Ngắt Thu Thập Real-time";
        }

        private void Log(string msg)
        {
            if (textLog != null)
            {
                textLog.text += $"\n{msg}";
                
                // Tránh Overlap dồn khung Canvas làm đơ quá nhiều chữ
                if (textLog.text.Length > 2500)
                {
                    textLog.text = textLog.text.Substring(textLog.text.Length - 2000);
                }
                Debug.Log(msg);
            }
        }
    }
}
