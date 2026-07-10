using System;

namespace RagChatbot.BLL.DTOs
{
    public class PackageDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public long TokenQuota { get; set; }
        public string AllowedModels { get; set; }   // CSV các model id
        public int DurationDays { get; set; } = 30;
        public bool IsActive { get; set; } = true;

        // Tiện cho UI: tách CSV thành danh sách model
        public string[] ModelList =>
            (AllowedModels ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
