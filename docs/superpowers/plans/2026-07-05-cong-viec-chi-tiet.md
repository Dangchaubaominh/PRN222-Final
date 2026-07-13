# Final Project — Công việc chi tiết từng thành viên

**Repo:** https://github.com/Dangchaubaominh/PRN222-Final · **Ngày:** 2026-07-05
Kèm theo: [phân công 4 người](2026-07-05-phan-cong-4-nguoi.md) · [spec thiết kế](../specs/2026-07-05-final-project-design.md)

Doc này mô tả **cụ thể** việc của từng người: file cần tạo/sửa, và "xong là thế nào" (Definition of Done). Đọc **Mục 1 (Hợp đồng dùng chung) trước** — đó là các tên field/method mọi người phải thống nhất để làm song song không vỡ.

---

## 1. Hợp đồng dùng chung (CHỐT TRƯỚC KHI CODE)

### 1.1 Model ID (dùng đúng chuỗi này ở mọi nơi)
`gemini-2.5-flash-lite` · `gemini-2.5-flash` · `gemini-2.5-pro`

### 1.2 Entity & field (đặt tên đúng để 3 lớp khớp)
| Entity | Người tạo | Field chính |
|---|---|---|
| `Package` | Bảo Minh | `Id int`, `Name string`, `Price decimal` (VND), `TokenQuota long`, `AllowedModels string` (CSV model id), `DurationDays int`, `IsActive bool` |
| `UserSubscription` | Bảo Minh | `Id int`, `UserId int`, `PackageId int`, `StartAt DateTime`, `ExpireAt DateTime`, `TokensUsed long`, `Status string` ("Active"/"Expired") |
| `PaymentOrder` | Bảo Minh | `Id int`, `UserId int`, `PackageId int`, `Amount decimal`, `VnpTxnRef string` (unique), `Status string` ("Pending"/"Paid"/"Failed"), `CreatedAt DateTime`, `PaidAt DateTime?` |
| `TokenUsageLog` | Tân | `Id int`, `UserId int`, `SubjectId Guid?`, `Model string`, `PromptTokens int`, `CompletionTokens int`, `TotalTokens int`, `CreatedAt DateTime` |
| `SubjectChunkConfig` | Đại | `SubjectId Guid` (PK+FK), `MaxWordsPerChunk int` (=400), `OverlapSentences int` (=2), `Strategy string` ("Semantic"/"Fixed") |
| `BenchmarkRun` | Đại | `Id int`, `CreatedById int`, `SubjectId Guid?`, `CreatedAt DateTime` |
| `BenchmarkResult` | Đại | `Id int`, `RunId int` (FK), `Model string`, `Question string`, `Answer string`, `LatencyMs int`, `TotalTokens int`, `EstimatedCost decimal` |

### 1.3 Chữ ký service liên lớp (điểm giao giữa các người)
```csharp
// Bảo Minh cung cấp — Tân & Vũ dùng
UserSubscription? ISubscriptionService.GetActive(int userId);      // gói đang hiệu lực (null nếu không có)
long ISubscriptionService.GetRemainingQuota(int userId);           // TokenQuota - TokensUsed (0 nếu hết/không gói)
void ISubscriptionService.AddUsedTokens(int userId, long tokens);  // cộng token sau mỗi câu chat

// Tân cung cấp — Vũ (thống kê) dùng
void ITokenUsageService.Log(int userId, Guid? subjectId, string model, int prompt, int completion);

// Đại cung cấp — dùng trong pipeline xử lý tài liệu
SubjectChunkConfig IChunkConfigService.GetForSubject(Guid subjectId); // trả mặc định 400/2/Semantic nếu chưa cấu hình
```
> Nếu ai muốn đổi chữ ký, báo nhóm trước rồi sửa ở đây.

### 1.4 Đăng ký DI & migration
- Mọi service/repository mới đăng ký tại `RagChatbot.BLL/Extensions/ServiceCollectionExtensions.cs` (`AddProjectDependencies`).
- Mọi entity mới thêm `DbSet` vào `RagChatbot.DAL/Data/ApplicationDbContext.cs`.
- **Migration:** xem quy tắc ở Mục 6 — rất quan trọng khi 4 người cùng thêm entity.

---

## 2. Bảo Minh — Gói & Thanh toán MoMo

**Mục tiêu:** Học sinh mua gói (quota token/tháng) qua MoMo sandbox → sinh dữ liệu doanh thu.

**Task:**
1. **DAL** — tạo entity `Package`, `UserSubscription`, `PaymentOrder` (Mục 1.2) trong `RagChatbot.DAL/Entities/`; thêm `DbSet` vào `ApplicationDbContext`; tạo repository `IPackageRepository`, `ISubscriptionRepository`, `IPaymentRepository` (+ Impl). **Migration** `AddPackageAndPayment`.
2. **BLL** — `IPackageService` (`GetActive()`, `GetById(int)`, Admin: `Create/Update/ToggleActive`); `ISubscriptionService` (chữ ký ở 1.3 + `ActivateOrRenew(int userId, int packageId)`); DTO `PackageDto`.
3. **BLL** — `IPaymentService` → `MoMoService`: `Task<string> CreatePaymentUrl(int userId, int packageId, string clientIp)` (POST server→server tới MoMo lấy `payUrl`), `PaymentResult HandleReturn(IReadOnlyDictionary<string,string> momoParams)` (xác thực chữ ký HMAC-SHA256 theo thứ tự field cố định của MoMo, cập nhật `PaymentOrder`, gọi `ActivateOrRenew`).
4. **UI** — trang Admin `Pages/Package/` (danh sách/tạo/sửa gói); trang Học sinh `Pages/Package/Buy` (xem gói + nút mua); `Pages/Payment/Return` (nhận callback MoMo, báo kết quả); `Pages/Package/Mine` (Gói của tôi + lịch sử `PaymentOrder`).
5. **Seed** 3 gói: Free (0đ, 50k token, `flash-lite`), Basic (49k đ, 500k token, `flash-lite,flash`), Pro (99k đ, 2tr token, tất cả model).
6. Đăng ký DI; cấu hình `Momo:PartnerCode/AccessKey/SecretKey/Endpoint/RedirectUrl` (sandbox có **creds test công khai** — chạy ngay không cần đăng ký; production mới cần đăng ký merchant).

**Done khi:** mua gói ở sandbox → redirect MoMo → thanh toán thử → quay lại `Return` thấy "thành công", `UserSubscription` được tạo, `PaymentOrder=Paid`; Admin CRUD được gói.

## 3. Tân — Token & Chọn model

**Mục tiêu:** đếm token mỗi lần chat, chặn khi hết quota, cho chọn model theo gói.

**Task:**
1. **DAL** — entity `TokenUsageLog` (1.2) + `DbSet` + `ITokenUsageRepository`. **Migration** `AddTokenUsageLog`.
2. **BLL** — refactor `GeminiService`/`IAIService`: thêm tham số `string model` vào `GenerateChatResponseStreamAsync` và `GenerateContentAsync`; đọc khối `usageMetadata` (promptTokenCount / candidatesTokenCount) từ response Gemini và trả về cho tầng gọi.
3. **BLL** — `ITokenUsageService.Log(...)` (1.3) + `long TotalTokens(int userId, DateTime from, DateTime to)`.
4. **Chặn quota** — trong `ChatHub.StreamMessage` (hoặc `ChatbotService`): trước khi hỏi, kiểm tra `ISubscriptionService.GetRemainingQuota(userId) > 0`; nếu hết → gửi `ReceiveError` "Hết quota, vui lòng nâng cấp gói". Sau khi trả lời xong → `TokenUsageService.Log(...)` + `SubscriptionService.AddUsedTokens(...)`.
5. **UI** — thêm dropdown chọn model vào khung chat `Pages/Chat/Index.cshtml` (chỉ hiện model trong `Package.AllowedModels` của gói người dùng); gửi `model` kèm câu hỏi qua SignalR.

**Done khi:** chat trừ đúng token theo `usageMetadata`; đổi model chạy được; hết quota bị chặn có thông báo.

## 4. Vũ — Thống kê & Báo cáo

**Mục tiêu:** dashboard số liệu theo vai trò.

**Task:**
1. **DAL** — thêm query tổng hợp vào repository của Bảo Minh/Tân (hoặc tạo `IReportRepository`): doanh thu theo ngày/tuần/tháng từ `PaymentOrder` (Status=Paid); token theo tuần/tháng/model từ `TokenUsageLog`.
2. **BLL** — `IStatisticsService`: `GetRevenue(from,to,granularity)`, `GetTokenUsage(from,to)`, `GetLecturerSubjectReport(Guid subjectId)`, `GetStudentUsage(int userId)` + các DTO report.
3. **UI Admin** — `Pages/Statistics/Index`: biểu đồ doanh thu (đường/cột) + token toàn hệ thống (theo model), nút **xuất CSV**.
4. **UI Giảng viên** — theo môn: số câu hỏi, tài liệu hay bị hỏi, học sinh tích cực.
5. **UI Học sinh** — quota còn lại + token đã dùng tuần/tháng (nối vào `Pages/Package/Mine` hoặc trang riêng).
> **Không chờ Bảo Minh/Tân:** dựng service + UI với **dữ liệu seed/mock** trước, đổi sang query thật khi 2 người kia xong.

**Done khi:** 3 dashboard hiển thị đúng số (thật hoặc mock), có Chart.js + xuất CSV cho Admin.

## 5. Đại — Chunk config + Citation + Benchmark

**Mục tiêu:** 3 feature nhỏ độc lập.

**Task — Chunk config (P2):**
1. **DAL** — entity `SubjectChunkConfig` (1.2) + `DbSet` + repository. **Migration** `AddSubjectChunkConfig`.
2. **BLL** — `IChunkConfigService.GetForSubject(Guid)` (1.3, trả mặc định nếu chưa có) + `Save(...)`; sửa `DocumentProcessingService` để lấy config theo môn rồi truyền vào `SemanticChunker.SplitText(text, cfg.MaxWordsPerChunk, cfg.OverlapSentences)`.
3. **UI** — `Pages/Subject/ChunkConfig` (Admin cấu hình mỗi môn); **ẩn** ô cấu hình chunk khỏi luồng upload của Giảng viên (`Pages/Document/Create`).

**Task — Citation (P5):**
4. Hoàn thiện hiển thị nguồn trong `Pages/Chat/Index` (tên tài liệu + trang + đoạn + snippet) + nút mở tài liệu gốc (link `Pages/Document/ViewDoc`).

**Task — Benchmark (P6, điểm cộng, làm sau cùng):**
5. **DAL** — `BenchmarkRun`, `BenchmarkResult` (1.2) + repository + **migration**.
6. **BLL** — `IBenchmarkService.Run(int adminId, Guid? subjectId, List<string> questions, List<string> models)`: gọi từng model qua `GeminiService` (cần refactor model của **Tân**), đo `LatencyMs`, `TotalTokens`, ước tính chi phí.
7. **UI** — `Pages/Benchmark/Index` (Admin): chọn bộ câu hỏi + môn → bảng + biểu đồ so sánh model.

**Done khi:** Admin đặt chunk theo môn (GV không thấy); citation hiển thị đầy đủ + mở được tài liệu; trang benchmark so sánh ≥2 model.

---

## 6. Quy tắc chống xung đột (ĐỌC KỸ)

1. **Migration EF (dễ vỡ nhất):** ai thêm entity → `git pull main` → `dotnet ef migrations add <Ten> --project RagChatbot.DAL --startup-project RagChatbot.RazorPages` → `dotnet ef database update` → build/test → **push ngay + báo nhóm**. Đừng để 2 migration chưa merge cùng lúc. Nếu kẹt: xoá migration của mình, pull, tạo lại.
2. **File dùng chung** (`ApplicationDbContext`, `ServiceCollectionExtensions`, `_Layout` nav): conflict nhỏ — luôn `git pull --rebase origin main` trước khi push.
3. **Secret** (`appsettings.json`, MoMo HashSecret, Gemini key): để User Secrets / file gitignore. **Không commit.**

## 7. Quy trình git (mỗi nhiệm vụ)

```bash
git checkout main && git pull
git checkout -b <mang>-<viec>-<ten>       # vd: goi-momo-service-baominh
# ... code ...
dotnet build && dotnet test               # xanh mới mở PR
git add -A && git commit -m "feat(<mang>): <mô tả>"
git pull --rebase origin main
git push -u origin <mang>-<viec>-<ten>    # mở Pull Request → review → merge main
```

## 8. Thứ tự khởi động

- **Ngay & song song:** Bảo Minh (Gói), Tân (Gemini refactor + TokenLog), Đại (Chunk config + Citation).
- **Kế:** Vũ (Thống kê, mock trước); Tân nối chọn model + chặn quota sau khi Bảo Minh có gói.
- **Cuối:** ghép số thật cho Thống kê; Đại làm Benchmark (cần refactor model của Tân).
