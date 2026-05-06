using System;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Firebase.Firestore;
using Suhdo.FSM.Profile.Models;

namespace Suhdo.FSM.Sample.FriendChat
{
    using Models_UserProfile = Suhdo.FSM.Profile.Models.UserProfile;

    // ĐỊNH NGHĨA PROFILE TÙY CHỈNH CHO PROJECT NÀY
    [FirestoreData]
    public class MyGameProfile : Models_UserProfile
    {
        [FirestoreProperty("level")] public int Level { get; set; } = 1;
        [FirestoreProperty("score")] public long Score { get; set; } = 0;
    }

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
            Log("Đang tải danh sách kết bạn (Dùng chiến lược Lazy Load)...");
            var friendList = await FirebaseInit.FriendService.FetchAllFriendsAsync();
            if (friendList == null || friendList.Count == 0)
            {
                Log("=> Danh sách bạn bè trống.");
                return;
            }
            
            // [Chiến lược Lazy Load]
            var uids = friendList.ConvertAll(f => f.Uid);

            // Ép kiểu sang IProfileService<MyGameProfile> để lấy các trường tùy chỉnh
            var profileService = FirebaseInit.ProfileService as Suhdo.FSM.Profile.IProfileService<MyGameProfile>;
            if (profileService == null)
            {
                Log("=> Lỗi: ProfileService không khớp với MyGameProfile.");
                return;
            }

            var profiles = await profileService.FetchPublicProfilesAsync(uids);
            var statuses = await FirebaseInit.PresenceService.GetStatusesAsync(uids);

            Log($"--- Danh sách bạn ({friendList.Count} người) ---");
            foreach (var f in friendList)
            {
                string statusIcon = "⚪ Offline";
                if (statuses.TryGetValue(f.Uid, out var p) && p.IsOnline)
                {
                    statusIcon = "🟢 Online";
                }
                
                if (profiles.TryGetValue(f.Uid, out var profile))
                {
                    // Giờ đây chúng ta có thể truy cập .Level và .Score của MyGameProfile
                    Log($"{statusIcon} | Name: {profile.DisplayName} | Lvl: {profile.Level} | Score: {profile.Score} | Status: {f.Status}");
                }
                else
                {
                    Log($"{statusIcon} | UID: {f.Uid} (Profile lỗi) | Status: {f.Status}");
                }
            }
        }

        private async Task<string> ResolveInputToUidAsync()
        {
            string input = TargetId;
            if (string.IsNullOrEmpty(input)) return null;

            if (input.Length <= 8)
            {
                 Log($"Đang phân giải Token {input} về dạng UID gốc...");
                 var targetProfile = await FirebaseInit.ProfileService.FindProfileByFriendCodeAsync(input);
                 if (targetProfile != null) return targetProfile.Uid;
                 
                 Log($"=> KHÔNG TÌM THẤY MÃ AI LÀ: {input}!");
                 return null;
            }
            return input;
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
            
            bool success = await FirebaseInit.FriendService.SendFriendRequestAsync(finalTargetUid);
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
