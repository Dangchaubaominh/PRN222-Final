using System.Collections.Generic;

namespace RagChatbot.BLL.DTOs
{
    // Một điểm dữ liệu theo nhãn (ngày / tên) cho biểu đồ
    public record DailyRevenue(string Label, decimal Amount);
    public record DailyToken(string Label, long Tokens);
    public record RevenueByPackage(string Package, decimal Amount, int Orders);
    public record TokenByModel(string Model, long Tokens);
    public record SubjectStat(string Subject, int Questions, long Tokens);

    // Thống kê cho Admin
    public class AdminStats
    {
        public decimal TotalRevenue { get; set; }
        public int PaidOrders { get; set; }
        public long TotalTokens { get; set; }
        public List<DailyRevenue> RevenueByDay { get; set; } = new();
        public List<RevenueByPackage> RevenueByPackage { get; set; } = new();
        public List<DailyToken> TokensByDay { get; set; } = new();
        public List<TokenByModel> TokensByModel { get; set; } = new();
    }

    // Thống kê cho Giảng viên (theo môn mình phụ trách)
    public class LecturerStats
    {
        public List<SubjectStat> Subjects { get; set; } = new();
    }
}
