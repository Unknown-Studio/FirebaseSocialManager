using System.Threading;
using Cysharp.Threading.Tasks;

namespace SocialManager.SaveGame
{
    public interface ISaveGameService
    {
        /// <summary>
        /// Lưu trạng thái Snapshot hiện tại của Game lên Firestore (Ghi đè - Overwrite).
        /// Kiểu T phải được đánh dấu Attribute của Firestore.
        /// </summary>
        UniTask<bool> SaveAsync<T>(T data, CancellationToken cancellationToken = default);

        /// <summary>
        /// Tải dữ liệu Save Game từ Cloud (Bản ghi duy nhất).
        /// </summary>
        /// <typeparam name="T">Class chứa dữ liệu Save của bạn.</typeparam>
        UniTask<T> LoadAsync<T>(CancellationToken cancellationToken = default) where T : class;

        /// <summary>
        /// Xóa dữ liệu Save Game của người chơi.
        /// </summary>
        UniTask<bool> DeleteSaveAsync(CancellationToken cancellationToken = default);
    }
}
