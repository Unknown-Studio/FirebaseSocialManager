using System.Threading;
using System.Threading.Tasks;

namespace Suhdo.FSM.SaveGame
{
    public interface ISaveGameService
    {
        /// <summary>
        /// Lưu trạng thái Snapshot hiện tại của Game lên Firestore (Ghi đè - Overwrite).
        /// Kiểu T phải được đánh dấu Attribute của Firestore.
        /// </summary>
        Task<bool> SaveAsync<T>(T data, CancellationToken cancellationToken = default);

        /// <summary>
        /// Tải dữ liệu Save Game từ Cloud (Bản ghi duy nhất).
        /// </summary>
        /// <typeparam name="T">Class chứa dữ liệu Save của bạn.</typeparam>
        Task<T> LoadAsync<T>(CancellationToken cancellationToken = default) where T : class;

        /// <summary>
        /// Xóa dữ liệu Save Game của người chơi.
        /// </summary>
        Task<bool> DeleteSaveAsync(CancellationToken cancellationToken = default);
    }
}
