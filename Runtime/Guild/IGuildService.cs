using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Suhdo.FSM.Team.Models;

namespace Suhdo.FSM.Team
{
    public interface IGuildService
    {
        Task<string> CreateGuildAsync(string name, string description, string joinType, string region, CancellationToken cancellationToken = default);
        Task<bool> JoinGuildAsync(string guildId, CancellationToken cancellationToken = default);
        Task<bool> LeaveGuildAsync(string guildId, CancellationToken cancellationToken = default);
        Task<GuildData> FetchGuildAsync(string guildId, CancellationToken cancellationToken = default);
        Task<List<GuildData>> SearchGuildsAsync(string queryText, CancellationToken cancellationToken = default);
        Task<List<GuildData>> GetSuggestedGuildsAsync(string region, CancellationToken cancellationToken = default);
        Task<List<GuildMember>> FetchMembersAsync(string guildId, CancellationToken cancellationToken = default);
        Task<bool> SendMessageAsync(string guildId, string text, CancellationToken cancellationToken = default);
        IDisposable ListenForNewMessages(string guildId, Action<GuildMessage> onMessageAdded);
    }
}
