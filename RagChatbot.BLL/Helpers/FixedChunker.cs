using System;
using System.Collections.Generic;

namespace RagChatbot.BLL.Helpers
{
    /// <summary>
    /// Fixed Chunker — cắt cứng đúng N từ mỗi chunk theo thứ tự văn bản, không quan tâm
    /// ranh giới câu/đoạn. Dùng khi Strategy="Fixed": đảm bảo kích thước chunk đồng đều
    /// tuyệt đối theo MaxWordsPerChunk, đánh đổi lấy khả năng cắt ngang giữa câu
    /// (khác với SemanticChunker — ưu tiên không cắt câu, chấp nhận kích thước không đều).
    /// </summary>
    public static class FixedChunker
    {
        /// <param name="text">Nội dung văn bản đầu vào</param>
        /// <param name="maxWordsPerChunk">Số từ mỗi chunk (chunk cuối có thể ngắn hơn nếu không chia hết)</param>
        /// <param name="overlapWords">Số từ cuối chunk trước lặp lại ở đầu chunk sau, làm ngữ cảnh nối tiếp</param>
        public static List<string> SplitText(string text, int maxWordsPerChunk = 400, int overlapWords = 0)
        {
            var chunks = new List<string>();
            if (string.IsNullOrWhiteSpace(text) || maxWordsPerChunk <= 0)
                return chunks;

            var words = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0)
                return chunks;

            overlapWords = Math.Clamp(overlapWords, 0, maxWordsPerChunk - 1);
            int step = maxWordsPerChunk - overlapWords;

            for (int start = 0; start < words.Length; start += step)
            {
                int count = Math.Min(maxWordsPerChunk, words.Length - start);
                chunks.Add(string.Join(" ", words, start, count));

                if (start + count >= words.Length) break;
            }

            return chunks;
        }
    }
}
