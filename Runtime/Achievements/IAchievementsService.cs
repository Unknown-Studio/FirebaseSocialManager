using System.Threading;
using System.Threading.Tasks;

namespace SocialManager.Achievements
{
    public interface IAchievementsService<T> where T : class, new()
    {
        Task<T> FetchAchievementsAsync(string userId, CancellationToken cancellationToken = default);
        Task<bool> UpdateAchievementsAsync(T achievements, CancellationToken cancellationToken = default);
    }
}
