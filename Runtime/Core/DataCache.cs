using System;

namespace Suhdo.FSM.Core
{
    /// <summary>
    /// Lớp hỗ trợ lưu trữ dữ liệu tạm thời với thời gian hết hạn (TTL).
    /// </summary>
    /// <typeparam name="T">Loại dữ liệu cần cache</typeparam>
    public class DataCache<T>
    {
        private T _data;
        private DateTime _lastUpdateTime;
        private readonly TimeSpan _cacheDuration;

        public DataCache(TimeSpan cacheDuration)
        {
            _cacheDuration = cacheDuration;
            _lastUpdateTime = DateTime.MinValue;
        }

        /// <summary>
        /// Kiểm tra xem cache đã hết hạn chưa.
        /// </summary>
        public bool IsExpired => DateTime.Now - _lastUpdateTime > _cacheDuration;

        /// <summary>
        /// Lấy dữ liệu từ cache.
        /// </summary>
        public T Data => _data;

        /// <summary>
        /// Cập nhật dữ liệu mới vào cache và reset thời gian.
        /// </summary>
        /// <param name="newData">Dữ liệu mới</param>
        public void Update(T newData)
        {
            _data = newData;
            _lastUpdateTime = DateTime.Now;
        }

        /// <summary>
        /// Xóa cache (ép buộc refresh lần sau).
        /// </summary>
        public void Invalidate()
        {
            _lastUpdateTime = DateTime.MinValue;
        }
    }
}
