# Final Project — Phân công công việc 4 thành viên (theo function)

**Repo:** https://github.com/Dangchaubaominh/PRN222-Final · **Ngày:** 2026-07-05
**Spec:** [../specs/2026-07-05-final-project-design.md](../specs/2026-07-05-final-project-design.md)

Chia theo **function** (mỗi người sở hữu một mảng tính năng xuyên suốt 3 lớp DAL→BLL→UI). Ưu điểm: chạy song song, ít block nhau, contribution mỗi người là một feature trọn vẹn → dễ chấm.

## Bốn mảng

| # | Thành viên | Mảng sở hữu | Quy mô |
|---|---|---|---|
| **1** | **Bảo Minh** | Gói & Thanh toán VNPay (doanh thu) | Lớn |
| **2** | **Tân** | Token & Chọn model | Lớn |
| **3** | **Vũ** | Thống kê & Báo cáo | Lớn |
| **4** | **Đại** | Chunk config + Citation + Benchmark | 3 feature nhỏ ≈ 1 lớn |

## Trạng thái nền

- ✅ **P0** Foundation (baseline, build, test) — cả nhóm.
- ✅ **P1** Dashboard Chart.js — đã merge `main`.
- ⏳ 4 mảng dưới đây.

---

## Người 1 (Bảo Minh) — Gói & Thanh toán VNPay

**Mục tiêu:** Học sinh mua gói (quota token/tháng) qua VNPay sandbox; sinh dữ liệu doanh thu.

- **DAL:** entity `Package` (Name, Price, TokenQuota, AllowedModels, DurationDays, IsActive), `UserSubscription` (UserId, PackageId, StartAt, ExpireAt, TokensUsed, Status), `PaymentOrder` (UserId, PackageId, Amount, VnpTxnRef, Status, CreatedAt, PaidAt) + migration + repository.
- **BLL:** `PackageService` (CRUD, Admin), `SubscriptionService` (gói đang hoạt động, gia hạn), `IPaymentService` → `VnPayService` (tạo URL thanh toán, xác thực return/IPN, cập nhật `PaymentOrder`).
- **UI:** trang Admin quản lý Gói; trang Học sinh xem/mua Gói; trang Checkout + endpoint callback VNPay; trang "Gói của tôi".
- **Seed:** 3 gói mẫu Free / Basic / Pro.

**Phụ thuộc:** không (tự chứa). **Bắt đầu ngay.**

## Người 2 (Tân) — Token & Chọn model

**Mục tiêu:** đo token mỗi lần chat, chặn khi hết quota, cho chọn model theo gói.

- **DAL:** entity `TokenUsageLog` (UserId, SubjectId?, Model, PromptTokens, CompletionTokens, TotalTokens, CreatedAt) + migration + repository.
- **BLL:** refactor `GeminiService` nhận tham số `model` + đọc `usageMetadata` (prompt/completion tokens); `TokenUsageService` (ghi log, tổng token trong kỳ); chặn quota (kiểm tra `TokensUsed < TokenQuota` trước khi chat, cộng token sau mỗi câu trả lời).
- **UI:** bộ chọn model trong khung chat (chỉ model gói cho phép); thông báo hết quota / gợi ý nâng cấp.

**Phụ thuộc:** đọc `UserSubscription.TokensUsed`/`Package.AllowedModels` của **Người 1** (thống nhất tên field sớm). Có thể bắt đầu phần refactor Gemini + TokenUsageLog ngay.

## Người 3 (Vũ) — Thống kê & Báo cáo

**Mục tiêu:** dashboard số liệu cho từng vai trò.

- **DAL:** query tổng hợp trong repository (doanh thu theo ngày/tuần/tháng từ `PaymentOrder`; token theo tuần/tháng/model từ `TokenUsageLog`).
- **BLL:** `StatisticsService` (doanh thu, token theo model/môn, theo môn cho Giảng viên).
- **UI:** dashboard **Admin** (doanh thu + token toàn hệ thống, Chart.js, xuất CSV); **Giảng viên** (theo môn: số câu hỏi, tài liệu hay hỏi); **Học sinh** (quota còn lại + token tuần/tháng).

**Phụ thuộc:** dữ liệu của Người 1 & 2. **Cách làm:** dựng service + UI với **dữ liệu seed/mock trước**, ghép query thật khi Người 1&2 xong. Không chờ.

## Người 4 (Đại) — Chunk config + Citation + Benchmark

**Mục tiêu:** 3 feature nhỏ độc lập.

- **Chunk config (P2):** entity `SubjectChunkConfig` (SubjectId, MaxWordsPerChunk, OverlapSentences, Strategy) + migration + repo; `ChunkConfigService`; refactor `SemanticChunker`/`DocumentProcessingService` nhận config theo môn; trang Admin cấu hình chunk mỗi môn; **ẩn** ô cấu hình khỏi luồng upload của Giảng viên.
- **Citation (P5):** hoàn thiện hiển thị nguồn (tên tài liệu + trang + đoạn + snippet) + nút mở tài liệu gốc.
- **Benchmark (P6, điểm cộng):** entity `BenchmarkRun`/`BenchmarkResult`; `BenchmarkService` (chạy bộ câu hỏi qua nhiều model, đo latency/token/chi phí); trang Admin so sánh (bảng + biểu đồ).

**Phụ thuộc:** Benchmark cần `GeminiService` (model param) của **Người 2** → làm benchmark **sau cùng**. Chunk config + Citation làm ngay.

---

## Thứ tự đề xuất

1. **Ngay:** Người 1 (Gói), Người 2 (Gemini refactor + TokenLog), Người 4 (Chunk config + Citation) — song song.
2. **Kế:** Người 3 (Thống kê, mock trước) + Người 2 (chặn quota, chọn model sau khi Người 1 có gói).
3. **Cuối:** ghép số thật cho Thống kê; Người 4 làm Benchmark.

## 2 quy tắc tránh xung đột file chung

1. **Migration EF (quan trọng nhất):** ai thêm entity thì `git pull main` → `dotnet ef migrations add <Ten>` → build/test → push & báo nhóm **ngay**. Tránh 2 migration cùng lúc chưa merge (dễ lệch snapshot). Nếu kẹt: người sau xoá migration của mình, pull, tạo lại.
2. **File dùng chung** (`ApplicationDbContext` thêm `DbSet`, `ServiceCollectionExtensions` đăng ký DI, nav `_Layout`): conflict nhỏ, ai chạm thì `pull --rebase` trước khi push.

## Quy trình git (mỗi nhiệm vụ)

```bash
git checkout main && git pull
git checkout -b <mang>-<viec>-<ten>     # vd: goi-vnpay-service-minh
# ... code ...
dotnet build && dotnet test             # xanh mới mở PR
git add -A && git commit -m "feat(<mang>): <mô tả>"
git pull --rebase origin main
git push -u origin <mang>-<viec>-<ten>  # mở Pull Request để review & merge
```

- 1 nhiệm vụ = 1 nhánh + 1 PR → `main` luôn build được.
- Secret (`appsettings.json`, VNPay key) để ở User Secrets / file gitignore — không commit.
