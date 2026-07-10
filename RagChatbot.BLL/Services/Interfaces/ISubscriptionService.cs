using System.Threading.Tasks;

namespace RagChatbot.BLL.Services.Interfaces
{
    /// <summary>
    /// STUB TẠM — Chờ Bảo Minh implement ISubscriptionService thật.
    /// Interface này sẽ được xóa và thay bằng implementation thật của Bảo Minh.
    /// Tân tạo để ChatHub compile và test chức năng token logging trước.
    ///
    /// Khi Bảo Minh xong:
    ///   1. Xóa file này (ISubscriptionService_Stub.cs)
    ///   2. Đổi using trong ChatHub sang namespace của Bảo Minh
    ///   3. Xóa đăng ký stub trong ServiceCollectionExtensions
    /// </summary>
    public interface ISubscriptionService
    {
        /// <summary>Trả về quota token còn lại. 0 = hết quota / không có gói.</summary>
        long GetRemainingQuota(int userId);

        /// <summary>Cộng số token đã dùng vào subscription của user.</summary>
        void AddUsedTokens(int userId, long tokens);
    }
}
