# Dataset Benchmark 50 Câu Hỏi

Thư mục này chứa toàn bộ dữ liệu 50 câu hỏi trắc nghiệm và ground truth được trích xuất từ tài liệu **"Sách Tự Học .NET Toàn Tập - Tập 2: C# và .NET Framework"**.

Bộ dataset này được thiết kế để sử dụng cho Module Nghiên cứu RBL (RAG Benchmark) nhằm đánh giá các mô hình ngôn ngữ (LLM) và các kỹ thuật RAG.

## Cấu trúc files
- `test_set_50.csv`: Dữ liệu gốc ở định dạng bảng CSV (có thể import vào DB hoặc code).
- `test_set_50.xlsx`: Dữ liệu gốc ở định dạng Excel (dễ đọc và chỉnh sửa bởi con người).
- `test_set_50.json`: Dữ liệu đầy đủ ở định dạng JSON (dành cho API).
- `ragas_dataset.json`: Dữ liệu được format sẵn chuẩn theo form của thư viện **RAGAS** (chỉ gồm `question` và `ground_truth`), sẵn sàng để chạy đánh giá.

## Quy trình
- **Giai đoạn 1**: 25 câu hỏi đầu tiên.
- **Giai đoạn 2**: 25 câu hỏi tiếp theo.
(Cả 2 giai đoạn đã được gộp chung vào tập tin 50 câu này cho dễ sử dụng).
