using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using SocialManager.Team.Models;

namespace SocialManager.Team
{
    public interface IGuildService
    {
        UniTask<string> CreateGuildAsync(string name, string description, string joinType, string region, CancellationToken cancellationToken = default);
        UniTask<bool> JoinGuildAsync(string guildId, CancellationToken cancellationToken = default);
        UniTask<bool> LeaveGuildAsync(string guildId, CancellationToken cancellationToken = default);
        UniTask<GuildData> FetchGuildAsync(string guildId, CancellationToken cancellationToken = default);
        UniTask<List<GuildData>> SearchGuildsAsync(string queryText, CancellationToken cancellationToken = default);
        UniTask<List<GuildData>> GetSuggestedGuildsAsync(string region, CancellationToken cancellationToken = default);
        UniTask<List<GuildMember>> FetchMembersAsync(string guildId, CancellationToken cancellationToken = default);
        UniTask<bool> SendMessageAsync(string guildId, string text, CancellationToken cancellationToken = default);
        IDisposable ListenForNewMessages(string guildId, Action<GuildMessage> onMessageAdded);
    }
}
