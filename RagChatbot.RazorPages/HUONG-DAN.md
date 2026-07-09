# Phần của Thành viên D — Presentation (assets & services)

Nội dung: `RagChatbot.RazorPages/` gồm `BackgroundTasks/`, `Services/`, `ViewComponents/`, `wwwroot/` (css, lib, bootstrap...), `tessdata/` (OCR).

## Cách push
1. `git clone <repo-url>` và `cd` vào repo.
2. Đặt git identity của bạn:
   ```bash
   git config user.name "Ten Cua Ban"
   git config user.email "email-github@example.com"
   ```
3. Giải nén nội dung gói này (thư mục `RagChatbot.RazorPages/`) vào **gốc repo** (ghép chung với phần của C, các thư mục con khác nhau nên không đè nhau).
4. Push:
   ```bash
   git pull --rebase origin main
   git add RagChatbot.RazorPages/
   git commit -m "chore(Web): import background worker, services, static assets"
   git push origin main
   ```

Xem thêm `DISTRIBUTION-README.md` ở gói tổng.
