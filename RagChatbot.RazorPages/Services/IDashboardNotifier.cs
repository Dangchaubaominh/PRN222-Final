namespace RagChatbot.RazorPages.Services
{
    /// <summary>
    /// Báo cho dashboard biết số liệu (môn/tài liệu/tài khoản) đã thay đổi
    /// để client tự nạp lại con số.
    /// </summary>
    public interface IDashboardNotifier
    {
        Task StatsChangedAsync();
    }
}
