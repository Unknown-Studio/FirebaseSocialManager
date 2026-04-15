using System;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Suhdo.FSM.Sample.FriendChat
{
    public class FriendTestUI : MonoBehaviour
    {
        private TMP_InputField _inputTargetId;
        private Button _btnFetch;
        private Button _btnSend;
        private Button _btnAccept;
        private Button _btnReject;
        private Button _btnRemove;
        private TextMeshProUGUI _txtLog;
        private StringBuilder _logBuilder = new StringBuilder();

        private void Awake()
        {
            // Auto-wire Cấu trúc uGUI Components tự sinh ra qua Script Tool
            _inputTargetId = transform.Find("InputTargetId").GetComponent<TMP_InputField>();
            
            _btnFetch = transform.Find("VerticalGroup/BtnFetch").GetComponent<Button>();
            _btnSend = transform.Find("VerticalGroup/BtnSend").GetComponent<Button>();
            _btnAccept = transform.Find("VerticalGroup/BtnAccept").GetComponent<Button>();
            _btnReject = transform.Find("VerticalGroup/BtnReject").GetComponent<Button>();
            _btnRemove = transform.Find("VerticalGroup/BtnRemove").GetComponent<Button>();
            
            _txtLog = transform.Find("LogPanel/Viewport/Content/TxtLog").GetComponent<TextMeshProUGUI>();

            // Setup Listeners Hook
            _btnFetch.onClick.AddListener(() => FetchFriends());
            _btnSend.onClick.AddListener(() => SendRequest());
            _btnAccept.onClick.AddListener(() => Respond(true));
            _btnReject.onClick.AddListener(() => Respond(false));
            _btnRemove.onClick.AddListener(() => RemoveFriend());

            Log("Hệ thống uGUI Friend Test Đã Sẵn Sàng!");
        }

        private string TargetId => _inputTargetId.text.Trim();

        private void Log(string message)
        {
            _logBuilder.AppendLine($"[{DateTime.Now:HH:mm:ss}] {message}");
            _txtLog.text = _logBuilder.ToString();
        }

        private async void FetchFriends()
        {
            Log("Đang tải danh sách kết bạn...");
            var list = await FirebaseInit.FriendService.FetchAllFriendsAsync();
            if (list == null || list.Count == 0)
            {
                Log("=> Danh sách bạn bè trống.");
                return;
            }

            Log($"--- Danh sách bạn ({list.Count} UID) ---");
            
            // Lấy danh sách UIDs để query trạng thái hàng loạt
            var uids = list.ConvertAll(f => f.Uid);
            var statuses = await FirebaseInit.PresenceService.GetStatusesAsync(uids);

            foreach (var f in list)
            {
                string statusIcon = "⚪ Offline";
                if (statuses.TryGetValue(f.Uid, out var p) && p.IsOnline)
                {
                    statusIcon = "🟢 Online";
                }
                
                Log($"{statusIcon} | Name: {f.FriendName} | Status: {f.Status}");
            }
        }

        // Helper Function tự động chuyển đổi mã FriendCode (6 ký tự) về UID thực của Firebase để query
        private async Task<string> ResolveInputToUidAsync()
        {
            string input = TargetId;
            if (string.IsNullOrEmpty(input)) return null;

            if (input.Length <= 8) // Độ dài chuẩn của code ngắn
            {
                 Log($"Đang phân giải Token {input} về dạng UID gốc...");
                 var targetProfile = await FirebaseInit.ProfileService.FindProfileByFriendCodeAsync(input);
                 if (targetProfile != null) return targetProfile.Uid;
                 
                 Log($"=> KHÔNG TÌM THẤY MÃ AI LÀ: {input}!");
                 return null;
            }
            return input; // Nếu chuỗi rất dài (28 ký tự), quy ước bạn đang copy paste UID trực tiếp từ console
        }

        private async void SendRequest()
        {
            string finalTargetUid = await ResolveInputToUidAsync();
            if (string.IsNullOrEmpty(finalTargetUid))
            {
                Log("Vui lòng nhập Friend Code (Mã ngắn) hoặc UID.");
                return;
            }

            Log($"Đang Request gửi lời mời kết bạn tới UID: {finalTargetUid}...");
            
            // Tìm Profile thật của đối phương để lấy Avatar, Tên đính kèm vào FriendRecord 
            var theirProfile = await FirebaseInit.ProfileService.FetchPublicProfileAsync(finalTargetUid);
            if (theirProfile == null)
            {
                Log("=> Lỗi không lấy được Profile đối phương.");
                return;
            }

            // Lấy Profile thật của chủ thể (My Session) 
            var myProfile = await FirebaseInit.ProfileService.FetchMyProfileAsync();
            if (myProfile == null)
            {
                 Log("=> Lỗi: Bản thân bạn chưa tạo Profile (Chưa gọi InitializeOrUpdateProfileAsync với server)");
                 return;
            }
            
            bool success = await FirebaseInit.FriendService.SendFriendRequestAsync(finalTargetUid, theirProfile, myProfile);
            Log(success ? "=> Gửi YÊU CẦU thành công! Vui lòng bấm Load Danh Sách để xem trạng thái 'pending_sent'" : "=> Lỗi: Gửi thất bại do trùng lặp hoặc lỗi mạng.");
        }

        private async void Respond(bool isAccept)
        {
            string finalTargetUid = await ResolveInputToUidAsync();
            if (string.IsNullOrEmpty(finalTargetUid))
            {
                Log("Vui lòng nhập Target ID.");
                return;
            }

            Log($"Đang {(isAccept ? "ĐỒNG Ý" : "TỪ CHỐI")} yêu cầu từ: {finalTargetUid}...");
            bool success = await FirebaseInit.FriendService.RespondToFriendRequestAsync(finalTargetUid, isAccept);
            Log(success ? "=> Phản hồi chốt thành công!" : "=> Lỗi vòng lặp: Phản hồi bị tắc.");
        }

        private async void RemoveFriend()
        {
            string finalTargetUid = await ResolveInputToUidAsync();
            if (string.IsNullOrEmpty(finalTargetUid))
            {
                Log("Vui lòng nhập Target ID/Mã Code.");
                return;
            }

            Log($"Đang xóa bạn bè / Hủy yêu cầu của: {finalTargetUid}...");
            bool success = await FirebaseInit.FriendService.RemoveFriendAsync(finalTargetUid);
            Log(success ? "=> Đã thủ tiêu mối quan hệ (Xóa)!" : "=> Lỗi chưa thao tác.");
        }
    }
}
