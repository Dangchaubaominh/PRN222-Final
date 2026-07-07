# 🎓 EduChatbot — Final Project (PRN222)

Nâng cấp từ Assignment 2: nền tảng học tập AI dùng **RAG**, thời gian thực **SignalR**, trên **ASP.NET Core 9 Razor Pages · PostgreSQL/pgvector · Google Gemini**.

## Tính năng Final Project (đang phát triển)
- Nâng cấp giao diện (Tabler + Chart.js)
- Governance chunk theo môn (Admin cấu hình, Giảng viên upload theo cấu hình)
- Chat có trích dẫn nguồn tài liệu
- Chức năng gói trả phí (VNPay) + quota token/tháng
- Chọn model theo gói + đo lường token
- Thống kê & báo cáo (doanh thu, token, theo môn, cá nhân)
- Benchmark so sánh model (điểm cộng)

## Tài liệu thiết kế
- Spec: [docs/superpowers/specs/2026-07-05-final-project-design.md](docs/superpowers/specs/2026-07-05-final-project-design.md)
- Kế hoạch: [docs/superpowers/plans/](docs/superpowers/plans/)

## Chạy dự án
1. Cài PostgreSQL + pgvector (`CREATE EXTENSION IF NOT EXISTS vector;`).
2. Copy `RagChatbot.RazorPages/appsettings.example.json` → `appsettings.json`, điền connection string + SMTP.
3. Đặt Gemini key qua User Secrets:
   ```bash
   dotnet user-secrets set "Gemini:ApiKey" "YOUR_KEY" --project RagChatbot.RazorPages
   ```
4. Tạo DB: `dotnet ef database update --project RagChatbot.DAL --startup-project RagChatbot.RazorPages`
5. Chạy: `cd RagChatbot.RazorPages && dotnet run`

## Kiến trúc 3 lớp
`RagChatbot.RazorPages (Presentation) → RagChatbot.BLL (Business) → RagChatbot.DAL (Data)`

## Tài khoản mặc định
Mật khẩu tất cả: `123`. Admin: `admin`. Giảng viên: `giangvien`, `gv_minh`, `gv_lan`. Sinh viên: `sinhvien`, `sv_bao`…

---
**PRN222 Final Project — FPT University**
