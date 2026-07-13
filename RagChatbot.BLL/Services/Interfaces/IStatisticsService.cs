using RagChatbot.BLL.DTOs;

namespace RagChatbot.BLL.Services.Interfaces
{
    public interface IStatisticsService
    {
        // Doanh thu + token toàn hệ thống trong `days` ngày gần nhất (Admin)
        AdminStats GetAdminStats(int days = 30);

        // Thống kê theo môn của một giảng viên
        LecturerStats GetLecturerStats(int userId);
    }
}
