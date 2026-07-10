using System;
using System.Threading.Tasks;

namespace RagChatbot.BLL.Services.Interfaces
{
    /// <summary>
    /// Ghi log token tiêu thụ và tính tổng — theo hợp đồng nhóm Mục 1.3.
    /// Được cung cấp bởi Tân; Vũ (Thống kê) dùng TotalTokens().
    /// </summary>
    public interface ITokenUsageService
    {
        /// <summary>
        /// Ghi một bản ghi token sau mỗi lần chat thành công.
        /// </summary>
        Task LogAsync(int userId, Guid? subjectId, string model, int promptTokens, int completionTokens);

        /// <summary>
        /// Tổng token của userId trong khoảng [from, to].
        /// Vũ dùng hàm này cho Dashboard Thống kê.
        /// </summary>
        Task<long> TotalTokens(int userId, DateTime from, DateTime to);
    }
}
