# Final Project — EduChatbot nâng cấp (PRN222)

**Ngày:** 2026-07-05
**Nền tảng:** ASP.NET Core 9 Razor Pages · Kiến trúc 3 lớp · SignalR · PostgreSQL + pgvector · Google Gemini
**Cơ sở:** Nâng cấp từ Assignment 2 (`E:/Project/PRN222-Assiment2`), giữ nguyên nền tảng & pattern.

---

## 1. Mục tiêu

Nâng cấp EduChatbot (Assignment 2) thành Final Project với: giao diện mới, governance chunk theo vai trò, chức năng gói trả phí (VNPay), đo lường token, thống kê/báo cáo, và benchmark model. Chia việc cho 4 thành viên để contribution GitHub đồng đều và trung thực.

## 2. Quyết định đã chốt

| Hạng mục | Quyết định |
|---|---|
| UI kit | **Tabler** (Bootstrap 5, MIT) ốp vào Razor layout hiện có |
| Biểu đồ | **Chart.js** |
| Thanh toán | **VNPay sandbox** (redirect + return URL + IPN), tách `IPaymentService` để thay MoMo sau |
| Gói | Học sinh mua; giới hạn **quota token/tháng**; reset đầu kỳ |
| Model | `gemini-2.5-flash-lite`, `gemini-2.5-flash`, `gemini-2.5-pro`; học sinh chọn trong danh sách gói cho phép |
| Đếm token | Đọc `usageMetadata` từ phản hồi Gemini (prompt + completion) |
| Chunk config | Theo **từng môn**; Admin đặt; Giảng viên không thấy/không sửa |
| Benchmark | Cả hai: học sinh chọn model theo gói + trang benchmark cho Admin |
| Thống kê | Admin: doanh thu + token toàn hệ thống; Giảng viên: theo môn; Học sinh: token cá nhân |
| Tiền tệ | VND |

## 3. Giả định

- Tài khoản do Admin tạo (như A2); học sinh đăng nhập rồi mua gói, không tự đăng ký.
- Mỗi học sinh chỉ có **1 gói hoạt động** tại một thời điểm; mua gói mới = gia hạn/thay thế.
- Cả 4 thành viên đều đã tham gia Assignment 2 → import baseline theo layer là hợp lệ.

## 4. Thay đổi mô hình dữ liệu (DAL)

Entity mới + migration:

- **Package** — `Id, Name, Price(decimal, VND), TokenQuota(long), AllowedModels(csv/json), DurationDays, IsActive`. Admin CRUD.
- **UserSubscription** — `Id, UserId, PackageId, StartAt, ExpireAt, TokensUsed(long), Status(Active/Expired)`. Gói đang hoạt động + token đã dùng trong kỳ.
- **PaymentOrder** — `Id, UserId, PackageId, Amount, VnpTxnRef, Status(Pending/Paid/Failed), CreatedAt, PaidAt`. Đối soát VNPay → nguồn **doanh thu**.
- **TokenUsageLog** — `Id, UserId, SubjectId?, Model, PromptTokens, CompletionTokens, TotalTokens, CreatedAt`. Ghi mỗi lần chat → nguồn **báo cáo token**.
- **SubjectChunkConfig** — `SubjectId(PK/FK), MaxWordsPerChunk, OverlapSentences, Strategy(Semantic/Fixed)`. Cấu hình chunk theo môn.
- **BenchmarkRun** — `Id, CreatedById, SubjectId?, CreatedAt` + **BenchmarkResult** — `Id, RunId, Model, Question, Answer, LatencyMs, TotalTokens, EstimatedCost`.

Bổ sung: cột `Model` vào `ChatMessage` (phục vụ thống kê giảng viên) nếu cần.

## 5. Module

### M1 — Nâng UI (Tabler)
Ốp theme Tabler vào `_Layout.cshtml`; làm lại sidebar/topbar/card/table; khung dashboard + Chart.js. **Giữ nguyên toàn bộ luồng cũ**, chỉ thay lớp trình bày.

### M2 — Governance chunk theo môn
- Admin: trang cấu hình chunk mỗi môn → ghi `SubjectChunkConfig`.
- `SemanticChunker.SplitText` (hiện hardcode 400 từ / 2 câu) → nhận tham số từ config môn.
- Luồng upload của Giảng viên tự lấy config môn; **ẩn hoàn toàn** phần cấu hình chunk khỏi Giảng viên.

### M3 — Chat có trích dẫn nguồn
Đã có ~80% ở A2. Hoàn thiện hiển thị nguồn (tên tài liệu + trang + đoạn + snippet) trong UI mới; thêm nút mở tài liệu gốc từ citation.

### M4 — Gói + Thanh toán VNPay
- Học sinh xem gói → mua → redirect VNPay sandbox → return/IPN xác nhận → tạo/gia hạn `UserSubscription`, set `PaymentOrder=Paid`.
- **Chặn quota**: trước khi chat kiểm tra `TokensUsed < TokenQuota` và gói còn hạn; hết → báo nâng cấp. Sau mỗi câu trả lời cộng token thực tế.
- `IPaymentService` → `VnPayService` (tách lớp để thay MoMo).

### M5 — Chọn model theo gói + đếm token
- `GeminiService` refactor: nhận tham số `model`; đọc `usageMetadata` (SSE cuối) → ghi `TokenUsageLog`.
- Học sinh chọn model trong danh sách gói cho phép.

### M6 — Thống kê & báo cáo
- **Admin:** doanh thu (ngày/tuần/tháng, theo gói, số đơn, top người mua) + token toàn hệ thống (tuần/tháng/model). Chart.js + xuất CSV.
- **Giảng viên:** theo môn — số câu hỏi, tài liệu bị hỏi nhiều, học sinh tích cực.
- **Học sinh:** quota còn lại, token dùng tuần/tháng, lịch sử mua gói.

### M7 (điểm cộng) — Benchmark cho Admin
Chọn bộ câu hỏi + môn → chạy song song qua các model → so sánh latency/token/chi phí ước tính/câu trả lời → bảng + biểu đồ; lưu `BenchmarkRun/Result`.

## 6. Ánh xạ 3 lớp (giữ đúng pattern A2)

- **DAL:** entity + repository mới + migration.
- **BLL:** `PackageService, SubscriptionService, PaymentService(IPaymentService→VnPayService), TokenUsageService, ChunkConfigService, StatisticsService, BenchmarkService`. Đăng ký DI tập trung tại `ServiceCollectionExtensions.AddProjectDependencies`.
- **Presentation:** Pages mới (Package, Checkout, Statistics, Benchmark, ChunkConfig) + endpoint callback VNPay. SignalR tái dùng cho dashboard realtime.

## 7. Phân công 4 thành viên (contribution đồng đều)

| Người | Mảng | Nội dung |
|---|---|---|
| **A** | DAL | Entity/migration mới (Package, UserSubscription, PaymentOrder, TokenUsageLog, SubjectChunkConfig, Benchmark) + repository |
| **B** | BLL | PackageService, SubscriptionService, VNPay PaymentService, TokenUsageService, ChunkConfigService |
| **C** | Presentation A | UI Tabler (M1) + Chat citation (M3) + trang Gói/Checkout (M4-FE) |
| **D** | Presentation B | Dashboard thống kê (M6) + Benchmark (M7) + trang cấu hình chunk (M2-FE) |

Cân lại khối lượng ~đều; ghi rõ phụ thuộc: A làm nền → B → C/D song song.

## 8. Seed repo (import Assignment 2)

Cả 4 đã cùng làm A2 → mỗi người commit đúng phần layer mình đã viết khi import baseline:
- A push DAL, B push BLL, C/D push Presentation (chia theo vùng đã làm).
- Sau baseline, contribution đều đến tự nhiên từ khối lượng nâng cấp mới (phần được chấm).
- **Không** cắt code cũ giả tạo để độn commit — phản ánh sai tác giả, dễ bị soi `git blame`.

## 9. Thứ tự xây

1. M1 UI Tabler → 2. M2 chunk governance → 3. M4 gói + VNPay → 4. M5 model + token log → 5. M6 thống kê → 6. M3 hoàn thiện citation → 7. M7 benchmark.

## 10. Ngoài phạm vi (YAGNI)

- Đa nhà cung cấp model (OpenAI, v.v.) — chỉ Gemini.
- Học sinh tự đăng ký tài khoản.
- Nhiều gói hoạt động đồng thời / mô hình credit không reset.
