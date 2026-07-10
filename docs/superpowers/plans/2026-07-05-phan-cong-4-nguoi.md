# Final Project — Phân công công việc 4 thành viên

**Repo:** https://github.com/Dangchaubaominh/PRN222-Final · **Ngày:** 2026-07-05
**Spec:** [../specs/2026-07-05-final-project-design.md](../specs/2026-07-05-final-project-design.md)

## Vai trò (theo layer đã chia ở P0)

| Ký hiệu | Layer chính | Thành viên |
|---|---|---|
| **A** | DAL (entity, migration, repository) | Dangchaubaominh *(điền tên)* |
| **B** | BLL (service nghiệp vụ) | *(điền tên)* |
| **C** | Presentation A (UI, trang gói, chat) | *(điền tên)* |
| **D** | Presentation B (dashboard, cấu hình, benchmark) | *(điền tên)* |

> Mỗi người vẫn commit trên layer mạnh của mình để contribution phản ánh đúng năng lực, nhưng khối lượng được cân cho ~đều.

## Trạng thái

- ✅ **P0** Foundation — baseline, build, test (cả nhóm).
- ✅ **P1** Dashboard Chart.js (đã merge `main`).
- ⏳ **P2–P6** còn lại (bảng dưới).

---

## Bảng phân công theo giai đoạn

### P2 — Chunk config theo môn *(Admin đặt thông số chunk, Giảng viên upload theo đó, không sửa)*
| Việc | Người |
|---|---|
| Entity `SubjectChunkConfig` (MaxWords, OverlapSentences, Strategy) + migration + repository | **A** |
| `ChunkConfigService` (CRUD, lấy theo môn) + refactor `SemanticChunker`/`DocumentProcessingService` nhận config theo môn | **B** |
| Trang Admin: cấu hình chunk cho từng môn | **D** |
| Ẩn ô cấu hình chunk khỏi luồng upload của Giảng viên | **C** |

### P3 — Gói + VNPay + Token + Chọn model *(lớn nhất — chia kỹ)*
| Việc | Người |
|---|---|
| Entity `Package`, `UserSubscription`, `PaymentOrder`, `TokenUsageLog` + migration + repository | **A** |
| `PackageService`, `SubscriptionService`, `TokenUsageService` (đọc `usageMetadata`), chặn quota | **B** |
| `IPaymentService` → `VnPayService` (tạo URL, xác thực return/IPN) | **B** |
| Refactor `GeminiService`: nhận tham số `model`, ghi token log | **B** |
| Trang danh sách Gói + Checkout + endpoint callback VNPay | **C** |
| Bộ chọn model trong chat (theo gói cho phép) + UI báo hết quota/nâng cấp | **C** |
| Trang "Gói của tôi" + lịch sử mua (Học sinh) | **A** *(FE nhẹ, cân tải cho A)* |
| Dữ liệu seed gói mẫu (Free/Basic/Pro) | **D** |

### P4 — Thống kê & báo cáo
| Việc | Người |
|---|---|
| Query/read-model tổng hợp (doanh thu, token theo tuần/tháng) trong repository | **A** |
| `StatisticsService` (doanh thu, token theo model/môn, theo môn cho GV) | **B** |
| Dashboard Admin: doanh thu + token toàn hệ thống (Chart.js, xuất CSV) | **D** |
| Dashboard Giảng viên: theo môn (số câu hỏi, tài liệu hay hỏi) | **D** |
| Trang Học sinh: quota còn lại + token tuần/tháng | **C** |

### P5 — Citation hoàn thiện
| Việc | Người |
|---|---|
| Hiển thị nguồn đẹp (tên tài liệu + trang + đoạn + snippet) + nút mở tài liệu gốc | **C** |
| Chỉnh service trả metadata nguồn nếu cần | **B** |

### P6 — Benchmark model *(điểm cộng)*
| Việc | Người |
|---|---|
| Entity `BenchmarkRun`, `BenchmarkResult` + migration + repository | **A** |
| `BenchmarkService` (chạy bộ câu hỏi qua nhiều model, đo latency/token/chi phí) | **B** |
| Trang Admin benchmark: chọn bộ câu hỏi + môn, bảng + biểu đồ so sánh | **D** |

---

## Tóm tắt tải mỗi người (~cân bằng)

- **A (DAL):** toàn bộ entity/migration/repository (P2,P3,P4,P6) + vài trang Học sinh nhẹ (Gói của tôi). Nhiều đầu việc nhỏ, ổn định.
- **B (BLL):** trục nghiệp vụ — chunk, gói, subscription, VNPay, token, thống kê, benchmark, refactor Gemini. Nặng nhất → ưu tiên bắt đầu sớm, các phần khác chờ B.
- **C (Presentation A):** trang gói/checkout, chọn model, quota UI, citation, trang token Học sinh, chỉnh upload GV.
- **D (Presentation B):** trang cấu hình chunk, mọi dashboard thống kê, benchmark UI, seed gói.

> Nếu thấy B quá tải: chuyển `TokenUsageService` hoặc `BenchmarkService` sang cặp làm chung B+D.

## Thứ tự & phụ thuộc

1. **P2** (nền governance) → 2. **P3** (gói/token — lõi doanh thu) → 3. **P4** (thống kê, cần dữ liệu P3) → 4. **P5** (citation) → 5. **P6** (benchmark).

Trong mỗi phase: **A làm entity/migration trước → B làm service → C/D làm UI song song.** C, D và P5 (citation) có thể chạy song song với P3/P4 vì ít phụ thuộc.

## Quy trình git (mỗi người, mỗi phase)

```bash
git checkout main && git pull
git checkout -b p3-<viec>-<ten>        # vd: p3-vnpay-service
# ... code ...
git add -A && git commit -m "feat(p3): <mô tả>"
git pull --rebase origin main
git push -u origin p3-<viec>-<ten>     # rồi mở Pull Request để review & merge
```

- Mỗi nhiệm vụ = 1 nhánh + 1 PR → dễ review, giữ `main` luôn build được.
- `dotnet build` + `dotnet test` xanh trước khi mở PR.
- Secret (`appsettings.json`, VNPay key) để ở User Secrets / file gitignore — không commit.
