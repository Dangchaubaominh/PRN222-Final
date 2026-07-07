# Final Project — Chỉ mục kế hoạch triển khai

**Spec nguồn:** [../specs/2026-07-05-final-project-design.md](../specs/2026-07-05-final-project-design.md)
**Ngày:** 2026-07-05

Dự án được chia thành chuỗi plan tuần tự. Mỗi plan ra được phần chạy/test được độc lập, và được thực thi tách biệt (mỗi plan một chu kỳ review).

## Thứ tự & trạng thái

| # | Plan | Module spec | Chủ lực | File | Trạng thái |
|---|---|---|---|---|---|
| 0 | Foundation (repo, baseline, test infra) | — | Cả nhóm | `2026-07-05-plan-00-foundation.md` | ✅ đã viết |
| 1 | UI Tabler + dashboard shell | M1 | C | *(viết sau P0)* | ⏳ |
| 2 | Chunk config theo môn | M2 | A+B+D | *(viết sau)* | ⏳ |
| 3 | Gói + VNPay + Token + chọn model | M4, M5 | A+B+C | *(viết sau)* | ⏳ |
| 4 | Thống kê & báo cáo | M6 | A+D | *(viết sau)* | ⏳ |
| 5 | Citation hoàn thiện | M3 | C | *(viết sau)* | ⏳ |
| 6 | Benchmark model (điểm cộng) | M7 | B+D | *(viết sau)* | ⏳ |

## Phân công 4 thành viên (contribution đồng đều)

- **A — DAL:** entity/migration/repository mới (chunk config, package, subscription, payment, token log, benchmark).
- **B — BLL:** service nghiệp vụ (package, subscription, VNPay payment, token usage, chunk config, benchmark).
- **C — Presentation A:** UI Tabler, trang Gói/Checkout, chat citation.
- **D — Presentation B:** dashboard thống kê, cấu hình chunk (FE), benchmark (FE).

Mỗi plan module ghi rõ task nào thuộc thành viên nào để cân contribution.

## Nguyên tắc thực thi

- Mỗi plan chạy theo `superpowers:subagent-driven-development` hoặc `executing-plans`.
- Commit thường xuyên, mỗi task một deliverable test được.
- Giữ đúng pattern 3 lớp của Assignment 2; đăng ký DI tập trung tại `ServiceCollectionExtensions.AddProjectDependencies`.
