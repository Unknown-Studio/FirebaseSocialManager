using System.Threading;
using Cysharp.Threading.Tasks;

namespace SocialManager.Achievements
{
    public interface IAchievementsService<T> where T : class, new()
    {
        UniTask<T> FetchAchievementsAsync(string userId, CancellationToken cancellationToken = default);
        UniTask<bool> UpdateAchievementsAsync(T achievements, CancellationToken cancellationToken = default);
    }
}
