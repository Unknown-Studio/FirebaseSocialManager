using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Firestore;
using Suhdo.FSM.Team.Models;
using UnityEngine;

namespace Suhdo.FSM.Team
{
    public class GuildService : IGuildService
    {
        private const string COLLECTION_GUILDS = "guilds";
        private const string COLLECTION_USERS = "users";
        private const int MAX_GUILD_MEMBERS = 50;

        private readonly FirebaseFirestore _db;
        private readonly FirebaseAuth _auth;

        public GuildService(FirebaseFirestore firestore, FirebaseAuth auth)
        {
            _db = firestore;
            _auth = auth;
        }

        private string CurrentUserId => _auth.CurrentUser?.UserId;

        public async Task<string> CreateGuildAsync(string name, string description, string joinType, string region, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(CurrentUserId)) return null;

            try
            {
                DocumentReference guildRef = _db.Collection(COLLECTION_GUILDS).Document();
                
                var guildData = new GuildData
                {
                    Name = name,
                    Description = description,
                    LeaderId = CurrentUserId,
                    MemberCount = 1, // Bang chủ mặc định là thành viên đầu tiên
                    JoinType = joinType,
                    Region = string.IsNullOrEmpty(region) ? "global" : region.ToLower(),
                    CreatedAt = FieldValue.ServerTimestamp
                };

                var memberData = new GuildMember
                {
                    UserId = CurrentUserId,
                    Role = GuildMemberRole.Leader,
                    JoinedAt = FieldValue.ServerTimestamp
                };

                // Dùng Batch để tạo cùng lúc Guild và subcollection members của guild (Transaction yêu cầu document tồn tại trước, batch thì không)
                WriteBatch batch = _db.StartBatch();
                batch.Set(guildRef, guildData);
                batch.Set(guildRef.Collection("members").Document(CurrentUserId), memberData);

                // Cập nhật trường guildId ở bản ghi User Profile
                DocumentReference userRef = _db.Collection(COLLECTION_USERS).Document(CurrentUserId);
                batch.Update(userRef, new Dictionary<string, object> { { "guildId", guildRef.Id } });

                await batch.CommitAsync();
                return guildRef.Id;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GuildService] Lỗi CreateGuildAsync: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> JoinGuildAsync(string guildId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(CurrentUserId) || string.IsNullOrEmpty(guildId)) return false;

            try
            {
                DocumentReference guildRef = _db.Collection(COLLECTION_GUILDS).Document(guildId);
                DocumentReference memberRef = guildRef.Collection("members").Document(CurrentUserId);
                DocumentReference userRef = _db.Collection(COLLECTION_USERS).Document(CurrentUserId);

                // Sử dụng Transaction để lấy chính xác memberCount ngay thời điểm hiện tại và tăng an toàn
                bool success = await _db.RunTransactionAsync(async transaction =>
                {
                    DocumentSnapshot guildSnapshot = await transaction.GetSnapshotAsync(guildRef);

                    if (!guildSnapshot.Exists) 
                        throw new Exception("Guild không tồn tại.");

                    var guild = guildSnapshot.ConvertTo<GuildData>();

                    if (guild.JoinType == GuildJoinType.InviteOnly)
                        throw new Exception("Guild này chỉ chấp nhận tham gia qua lời mời.");

                    if (guild.MemberCount >= MAX_GUILD_MEMBERS)
                        throw new Exception("Guild đã đầy thành viên.");

                    // Kiểm tra tránh tình huống spam bấm Join nhiều lần hoặc đã ở trong bang
                    DocumentSnapshot memberSnapshot = await transaction.GetSnapshotAsync(memberRef);
                    if (memberSnapshot.Exists)
                        throw new Exception("Bạn đã là thành viên của Guild này.");

                    var memberData = new GuildMember
                    {
                        UserId = CurrentUserId,
                        Role = GuildMemberRole.Member,
                        JoinedAt = FieldValue.ServerTimestamp
                    };

                    transaction.Set(memberRef, memberData);
                    transaction.Update(guildRef, new Dictionary<string, object> { { "memberCount", FieldValue.Increment(1) } });
                    transaction.Update(userRef, new Dictionary<string, object> { { "guildId", guildId } });

                    return true;
                });

                return success;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GuildService] Lỗi JoinGuildAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> LeaveGuildAsync(string guildId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(CurrentUserId) || string.IsNullOrEmpty(guildId)) return false;

            try
            {
                DocumentReference guildRef = _db.Collection(COLLECTION_GUILDS).Document(guildId);
                DocumentReference memberRef = guildRef.Collection("members").Document(CurrentUserId);
                DocumentReference userRef = _db.Collection(COLLECTION_USERS).Document(CurrentUserId);

                bool success = await _db.RunTransactionAsync(async transaction =>
                {
                    DocumentSnapshot guildSnapshot = await transaction.GetSnapshotAsync(guildRef);
                    if (!guildSnapshot.Exists) return false;

                    var guild = guildSnapshot.ConvertTo<GuildData>();
                    
                    // Ngăn chặn ngớ ngẩn (Leader rời nhưng Guild còn người)
                    if (guild.LeaderId == CurrentUserId && guild.MemberCount > 1)
                        throw new Exception("Bạn là Bang chủ hiện tại! Vui lòng nhường chức trước khi rời Bang.");

                    transaction.Delete(memberRef);

                    if (guild.MemberCount <= 1)
                    {
                        // Bang không còn thành viên -> Xóa
                        transaction.Delete(guildRef);
                        // Lưu ý: Các file con (messages, members cũ) trên Firestore sẽ lơ lửng nếu không bị xóa đệ quy. 
                        // Vì chúng ta dùng Firebase NoSQL, việc để mặc kệ subcollection lơ lửng không gây tốn quá nhiều chi phí
                        // Hoặc bạn sẽ xử lý việc xóa subcollection thông qua Firebase Functions trong môi trường thực.
                    }
                    else
                    {
                        // Trừ số lượng thành viên đi 1
                        transaction.Update(guildRef, new Dictionary<string, object> { { "memberCount", FieldValue.Increment(-1) } });
                    }
                    
                    transaction.Update(userRef, new Dictionary<string, object> { { "guildId", string.Empty } });

                    return true;
                });

                return success;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GuildService] Lỗi LeaveGuildAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<GuildData> FetchGuildAsync(string guildId, CancellationToken cancellationToken = default)
        {
            try
            {
                var doc = await _db.Collection(COLLECTION_GUILDS).Document(guildId).GetSnapshotAsync();
                if (!doc.Exists) return null;

                var guild = doc.ConvertTo<GuildData>();
                guild.GuildId = doc.Id;
                return guild;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GuildService] Lỗi FetchGuildAsync: {ex.Message}");
                return null;
            }
        }

        public async Task<List<GuildData>> SearchGuildsAsync(string queryText, CancellationToken cancellationToken = default)
        {
            try
            {
                // Firestore không hỗ trợ search full text (LIKE '%..%'). Để match theo prefix ta dùng khoảng cách mã Unicode '\uf8ff'
                Query query = _db.Collection(COLLECTION_GUILDS)
                    .OrderBy("name")
                    .StartAt(queryText)
                    .EndAt(queryText + "\uf8ff")
                    .Limit(20);

                QuerySnapshot snapshot = await query.GetSnapshotAsync();
                
                List<GuildData> results = new List<GuildData>();
                foreach (var doc in snapshot.Documents)
                {
                    var guild = doc.ConvertTo<GuildData>();
                    guild.GuildId = doc.Id;

                    if (guild.JoinType == GuildJoinType.Open)
                    {
                        results.Add(guild);
                    }
                }
                return results;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GuildService] Lỗi SearchGuildsAsync: {ex.Message}");
                return new List<GuildData>();
            }
        }

        public async Task<List<GuildData>> GetSuggestedGuildsAsync(string region, CancellationToken cancellationToken = default)
        {
            try
            {
                // Lưu ý: Query này đòi hỏi cấu hình Composite Index trong Firebase Console 
                // Index: joinType (ASC), region (ASC), memberCount (DESC)
                Query query = _db.Collection(COLLECTION_GUILDS)
                    .WhereEqualTo("joinType", GuildJoinType.Open)
                    .WhereEqualTo("region", region.ToLower())
                    .WhereLessThan("memberCount", MAX_GUILD_MEMBERS)
                    .OrderByDescending("memberCount")
                    .Limit(20);

                QuerySnapshot snapshot = await query.GetSnapshotAsync();

                List<GuildData> results = new List<GuildData>();
                foreach (var doc in snapshot.Documents)
                {
                    var guild = doc.ConvertTo<GuildData>();
                    guild.GuildId = doc.Id;
                    results.Add(guild);
                }
                return results;
            }
            catch (Exception ex)
            {
                // Nếu lần đầu gọi code này bị văng Exception (Index Failed). 
                // Firebase sẽ trả về link Create Index ở Exception Message -> Click Link đó trên Debug Console của Unity để tạo.
                Debug.LogError($"[GuildService] Lỗi GetSuggestedGuildsAsync: {ex.Message}");
                return new List<GuildData>();
            }
        }

        public async Task<List<GuildMember>> FetchMembersAsync(string guildId, CancellationToken cancellationToken = default)
        {
            try
            {
                QuerySnapshot snapshot = await _db.Collection(COLLECTION_GUILDS).Document(guildId).Collection("members")
                    .GetSnapshotAsync();

                List<GuildMember> members = new List<GuildMember>();
                foreach (var doc in snapshot.Documents)
                {
                    var member = doc.ConvertTo<GuildMember>();
                    member.UserId = doc.Id;
                    members.Add(member);
                }
                return members;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GuildService] Lỗi FetchMembersAsync: {ex.Message}");
                return new List<GuildMember>();
            }
        }

        public async Task<bool> SendMessageAsync(string guildId, string text, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(CurrentUserId) || string.IsNullOrEmpty(guildId)) return false;

            try
            {
                // Tối ưu UI: Lấy snapshot của displayName, tránh tình trạng Chat không thấy tên.
                // Đối với game Production, bạn có thể truyền thẳng bộ Cache Profile vào Service này thông qua Dependency.
                string senderName = "Unknown";
                DocumentSnapshot myProfileSnap = await _db.Collection(COLLECTION_USERS).Document(CurrentUserId).GetSnapshotAsync();
                if (myProfileSnap.Exists && myProfileSnap.ContainsField("displayName"))
                {
                    senderName = myProfileSnap.GetValue<string>("displayName");
                }

                DocumentReference msgRef = _db.Collection(COLLECTION_GUILDS).Document(guildId).Collection("messages").Document();
                var msgData = new GuildMessage
                {
                    SenderId = CurrentUserId,
                    SenderName = senderName, // Map vào JSON
                    Text = text,
                    Timestamp = FieldValue.ServerTimestamp
                };

                await msgRef.SetAsync(msgData);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GuildService] Lỗi SendMessageAsync: {ex.Message}");
                return false;
            }
        }

        public IDisposable ListenForNewMessages(string guildId, Action<GuildMessage> onMessageAdded)
        {
            Query query = _db.Collection(COLLECTION_GUILDS).Document(guildId).Collection("messages")
                .OrderByDescending("timestamp").Limit(1);

            Action<QuerySnapshot> listener = (snapshot) =>
            {
                if (snapshot == null) return;
                foreach (DocumentChange change in snapshot.GetChanges())
                {
                    if (change.ChangeType == DocumentChange.Type.Added)
                    {
                        var msg = change.Document.ConvertTo<GuildMessage>();
                        msg.MessageId = change.Document.Id;

                        // Xử lý trường hợp message mới gửi từ Local sẽ có Timestamp = null do ServerTimestamp chưa phản hồi
                        if (msg.Timestamp == null && change.Document.Metadata.HasPendingWrites)
                        {
                            msg.Timestamp = change.Document.GetValue<Timestamp>("timestamp", ServerTimestampBehavior.Estimate);
                        }

                        onMessageAdded?.Invoke(msg);
                    }
                }
            };

            var listenerRegistration = query.Listen(listener);
            return new ListenerDisposer(listenerRegistration);
        }

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
