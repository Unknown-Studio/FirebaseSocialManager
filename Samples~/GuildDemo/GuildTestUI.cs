using System;
using UnityEngine;
using System.Collections.Generic;
using Suhdo.FSM.Team.Models;
using TMPro;
using UnityEngine.UI;

namespace Suhdo.FSM.Sample.Guild
{
    public class GuildTestUI : MonoBehaviour
    {
        private string _currentGuildId;
        private IDisposable _messageListener;
        
        [Header("UI References")]
        [SerializeField] private TMP_InputField _guildName;
        [SerializeField] private TMP_InputField _guildDescription;
        [SerializeField] private TMP_InputField _guildSearchQuery;
        [SerializeField] private TMP_InputField _guildMessage;
        [SerializeField] private TMP_InputField _guildIdInput; // Thêm Input để nhập ID Guild cần thao tác
        [SerializeField] private TMP_Text textProfileLog;
        
        [Header("Buttons")]
        [SerializeField] private Button _btnCreateGuild;
        [SerializeField] private Button _btnSearchGuilds;
        [SerializeField] private Button _btnSendMessage;
        [SerializeField] private Button _btnFetch;
        [SerializeField] private Button _btnLeaveGuild;
        [SerializeField] private Button _btnJoinGuild;
        [SerializeField] private Button _btnFetchMembers;
        [SerializeField] private Button _btnFetchSuggest;
        [SerializeField] private Button _btnListenMessages;

        [Header("Test Data (For Context Menu / Fallback)")]
        public string MockGuildName = "Test Guild VN";
        public string MockDescription = "Hardcore gamers only";
        public string MockRegion = "vn";
        public string MockSearchQuery = "Test";
        public string MessageText = "Chào mọi người!";

        void Start()
        {
            // Bind UI Buttons
            if (_btnCreateGuild != null) _btnCreateGuild.onClick.AddListener(TestCreateGuildUI);
            if (_btnSearchGuilds != null) _btnSearchGuilds.onClick.AddListener(TestSearchGuildsUI);
            if (_btnSendMessage != null) _btnSendMessage.onClick.AddListener(TestSendMessageUI);
            if (_btnFetch != null) _btnFetch.onClick.AddListener(TestFetchGuildUI);
            if (_btnLeaveGuild != null) _btnLeaveGuild.onClick.AddListener(TestLeaveGuildUI);
            if (_btnJoinGuild != null) _btnJoinGuild.onClick.AddListener(TestJoinGuildUI);
            if (_btnFetchMembers != null) _btnFetchMembers.onClick.AddListener(TestFetchMembersUI);
            if (_btnListenMessages != null) _btnListenMessages.onClick.AddListener(TestListenForNewMessagesUI);
            if (_btnFetchSuggest != null) _btnFetchSuggest.onClick.AddListener(TestFetchSuggestedGuildsUI);
        }

        private void OnDestroy()
        {
            _messageListener?.Dispose();
        }

        private string GetTargetGuildId()
        {
            if (_guildIdInput != null && !string.IsNullOrEmpty(_guildIdInput.text))
                return _guildIdInput.text;
                
            if (!string.IsNullOrEmpty(_currentGuildId))
                return _currentGuildId;
                
            Log("<color=yellow>Vui lòng nhập Guild ID vào ô Input hoặc chạy Create Guild trước.</color>");
            return null;
        }

        public async void TestCreateGuildUI()
        {
            string name = _guildName != null && !string.IsNullOrEmpty(_guildName.text) ? _guildName.text : MockGuildName;
            string desc = _guildDescription != null && !string.IsNullOrEmpty(_guildDescription.text) ? _guildDescription.text : MockDescription;
            
            Log("--- Bắt đầu Tạo Bang ---");
            string guildId = await FirebaseInit.GuildService.CreateGuildAsync(name, desc, GuildJoinType.Open, MockRegion);
            if (!string.IsNullOrEmpty(guildId))
            {
                _currentGuildId = guildId;
                if (_guildIdInput != null) _guildIdInput.text = guildId;
                Log($"[Thành công] Đã tạo Guild với ID: {guildId}");
            }
            else
            {
                Log("<color=red>[Thất bại] KHÔNG thể tạo guild.</color>");
            }
        }

        public async void TestJoinGuildUI()
        {
            string targetGuildId = GetTargetGuildId();
            if (string.IsNullOrEmpty(targetGuildId)) return;
            
            Log($"--- Bắt đầu Xin Gia nhập Bang {targetGuildId} ---");
            bool success = await FirebaseInit.GuildService.JoinGuildAsync(targetGuildId);
            if (success)
            {
                _currentGuildId = targetGuildId;
                Log("[Thành công] Gia nhập guild thành công.");
            }
            else
            {
                Log("<color=red>[Thất bại] Không thể gia nhập guild.</color>");
            }
        }

        public async void TestLeaveGuildUI()
        {
            string targetGuildId = GetTargetGuildId();
            if (string.IsNullOrEmpty(targetGuildId)) return;
            
            Log($"--- Bắt đầu Rời khỏi Bang {targetGuildId} ---");
            bool success = await FirebaseInit.GuildService.LeaveGuildAsync(targetGuildId);
            if (success)
            {
                Log("[Thành công] Rời guild thành công.");
                if (_currentGuildId == targetGuildId) _currentGuildId = null;
            }
            else
            {
                 Log("<color=red>[Thất bại] Rời guild thất bại.</color>");
            }
        }

        public async void TestFetchGuildUI()
        {
            string targetGuildId = GetTargetGuildId();
            if (string.IsNullOrEmpty(targetGuildId)) return;

            Log($"--- Bắt đầu Lấy thông tin Bang {targetGuildId} ---");
            var guild = await FirebaseInit.GuildService.FetchGuildAsync(targetGuildId);
            if (guild != null)
            {
                Log($"[Thành công] Guild: {guild.Name} - Region: {guild.Region} - Members: {guild.MemberCount}/50");
            }
            else
            {
                Log("<color=red>[Thất bại] Không tìm thấy Guild.</color>");
            }
        }

        public async void TestFetchMembersUI()
        {
            string targetGuildId = GetTargetGuildId();
            if (string.IsNullOrEmpty(targetGuildId)) return;

            Log($"--- Bắt đầu Lấy danh sách thành viên Bang {targetGuildId} ---");
            var members = await FirebaseInit.GuildService.FetchMembersAsync(targetGuildId);
            Log($"Tìm thấy {members.Count} thành viên.");
            foreach (var mem in members)
            {
                Log($"- UserID: {mem.UserId} | Role: {mem.Role}");
            }
        }

        public async void TestSearchGuildsUI()
        {
             string query = _guildSearchQuery != null && !string.IsNullOrEmpty(_guildSearchQuery.text) ? _guildSearchQuery.text : MockSearchQuery;
            Log($"--- Bắt đầu Tìm kiếm '{query}' ---");
            List<GuildData> results = await FirebaseInit.GuildService.SearchGuildsAsync(query);
            Log($"Tìm thấy {results.Count} guilds.");
            foreach(var guild in results)
            {
                Log($"- Guild: {guild.Name} (ID: {guild.GuildId}) - Members: {guild.MemberCount}");
            }
        }

        public async void TestFetchSuggestedGuildsUI()
        {
            Log($"--- Bắt đầu Gợi ý Guilds vùng '{MockRegion}' ---");
            List<GuildData> results = await FirebaseInit.GuildService.GetSuggestedGuildsAsync(MockRegion);
            Log($"Tìm thấy {results.Count} guilds gợi ý.");
            foreach(var guild in results)
            {
                Log($"- Guild: {guild.Name} (ID: {guild.GuildId}) - Members: {guild.MemberCount}");
            }
        }

        public async void TestSendMessageUI()
        {
            string text = _guildMessage != null && !string.IsNullOrEmpty(_guildMessage.text) ? _guildMessage.text : MessageText;
            string targetGuildId = GetTargetGuildId();
            if (string.IsNullOrEmpty(targetGuildId)) return;
            
            Log($"--- Bắt đầu Gửi tin vào Bang {targetGuildId} ---");
            bool success = await FirebaseInit.GuildService.SendMessageAsync(targetGuildId, text);
            if (success)
                Log("[Thành công] Đã gửi tin nhắn.");
        }

        public void TestListenForNewMessagesUI()
        {
            string targetGuildId = GetTargetGuildId();
            if (string.IsNullOrEmpty(targetGuildId)) return;

            // Hủy listener cũ nếu có
            _messageListener?.Dispose();

            Log($"--- Bắt đầu Lắng nghe tin nhắn mới từ Bang {targetGuildId} ---");
            _messageListener = FirebaseInit.GuildService.ListenForNewMessages(targetGuildId, msg => 
            {
                Log($"<color=green>[Tin nhắn mới]</color> <b>{msg.SenderName}</b>: {msg.Text}");
            });
        }
        
        private void Log(string message)
        {
            if (textProfileLog != null)
            {
                textProfileLog.text += $"\n[{DateTime.Now:HH:mm:ss}] {message}";
            }
            Debug.Log(message);
        }
    }
}
