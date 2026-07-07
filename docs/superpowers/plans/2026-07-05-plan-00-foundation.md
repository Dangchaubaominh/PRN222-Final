# Plan 0 — Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Dựng repo Final Project từ baseline Assignment 2, build/chạy được, và có test project để các plan sau dùng TDD.

**Architecture:** Copy nguyên source Assignment 2 (3 project: DAL/BLL/RazorPages) sang thư mục Final Project, đổi tên solution, khởi tạo git với lịch sử baseline chia theo layer cho 4 thành viên, thêm test project xUnit tham chiếu BLL.

**Tech Stack:** .NET 9, EF Core 9.0.4, PostgreSQL + pgvector, xUnit.

## Global Constraints

- TargetFramework: `net9.0` (mọi project).
- Giữ đúng tên project cũ: `RagChatbot.DAL`, `RagChatbot.BLL`, `RagChatbot.RazorPages`.
- Không commit: `bin/`, `obj/`, `.vs/`, `wwwroot/uploads/`, `appsettings.json` (secrets), user secrets.
- Solution mới: `PRN222-FinalProject.sln`.
- Nguồn baseline: `E:/Project/PRN222-Assiment2`. Đích: `E:/Project/PRN222-FinalProject`.

---

### Task 1: Copy baseline & đổi tên solution

**Files:**
- Create: `E:/Project/PRN222-FinalProject/RagChatbot.DAL/**` (copy)
- Create: `E:/Project/PRN222-FinalProject/RagChatbot.BLL/**` (copy)
- Create: `E:/Project/PRN222-FinalProject/RagChatbot.RazorPages/**` (copy)
- Create: `E:/Project/PRN222-FinalProject/PRN222-FinalProject.sln`
- Create: `E:/Project/PRN222-FinalProject/.gitignore`

**Interfaces:**
- Consumes: source tree Assignment 2.
- Produces: solution `PRN222-FinalProject.sln` chứa 3 project, build được.

- [ ] **Step 1: Copy source, loại trừ artifacts**

Chạy trong Git Bash (robocopy chính xác hơn trên Windows cho việc loại trừ thư mục):

```bash
cd "E:/Project/PRN222-FinalProject"
for proj in RagChatbot.DAL RagChatbot.BLL RagChatbot.RazorPages; do
  robocopy "E:/Project/PRN222-Assiment2/$proj" "E:/Project/PRN222-FinalProject/$proj" /E \
    /XD bin obj .vs "$proj/wwwroot/uploads" \
    /XF *.user
done
# robocopy trả exit code >=8 mới là lỗi thật; 0-7 là bình thường
echo "copy done"
```

- [ ] **Step 2: Tạo solution mới và add 3 project**

```bash
cd "E:/Project/PRN222-FinalProject"
dotnet new sln -n PRN222-FinalProject
dotnet sln add RagChatbot.DAL/RagChatbot.DAL.csproj
dotnet sln add RagChatbot.BLL/RagChatbot.BLL.csproj
dotnet sln add RagChatbot.RazorPages/RagChatbot.RazorPages.csproj
```

- [ ] **Step 3: Tạo appsettings.example.json (không chứa secret)**

Copy `appsettings.json` thành `appsettings.example.json`, thay giá trị nhạy cảm bằng placeholder:

```bash
cp "RagChatbot.RazorPages/appsettings.json" "RagChatbot.RazorPages/appsettings.example.json"
```

Sau đó sửa `appsettings.example.json`: đặt `DefaultConnection` = `"Host=localhost;Database=educhatbot;Username=postgres;Password=YOUR_PASSWORD"`, xóa mật khẩu SMTP thật (đặt `"YOUR_APP_PASSWORD"`).

- [ ] **Step 4: Verify build**

```bash
cd "E:/Project/PRN222-FinalProject"
dotnet build PRN222-FinalProject.sln
```
Expected: `Build succeeded`. Nếu lỗi thiếu package → `dotnet restore` rồi build lại.

- [ ] **Step 5 (không commit — repo chưa init; commit ở Task 2).**

---

### Task 2: Init git + lịch sử baseline chia 4 layer

**Files:**
- Create: `E:/Project/PRN222-FinalProject/.gitignore`
- Create: `E:/Project/PRN222-FinalProject/README.md` (copy từ A2, đổi tiêu đề Final Project)

**Interfaces:**
- Consumes: solution build được từ Task 1.
- Produces: git repo với ≥5 commit baseline (skeleton + 4 layer), sẵn sàng push.

> **Lý do chia 4 commit:** cả 4 thành viên đã cùng làm Assignment 2, nên mỗi người commit đúng phần layer mình đã viết là phản ánh thật (không ngụy tạo). Mỗi thành viên chạy commit của mình bằng git identity của họ (`git config user.name/user.email`) trên cùng máy, hoặc lần lượt trên máy mỗi người sau khi clone.

- [ ] **Step 1: Tạo .gitignore**

Tạo `E:/Project/PRN222-FinalProject/.gitignore`:

```gitignore
bin/
obj/
.vs/
*.user
RagChatbot.RazorPages/appsettings.json
RagChatbot.RazorPages/wwwroot/uploads/
*.log
```

- [ ] **Step 2: Init repo + commit skeleton (người tạo repo)**

```bash
cd "E:/Project/PRN222-FinalProject"
git init -b main
git add .gitignore PRN222-FinalProject.sln README.md docs/
git commit -m "chore: khởi tạo solution Final Project + spec/plan"
```

- [ ] **Step 3: Commit layer DAL — thành viên A**

```bash
git config user.name "Ten Thanh Vien A"
git config user.email "email-a@example.com"
git add RagChatbot.DAL/
git commit -m "chore(DAL): import Data Access Layer từ Assignment 2"
```

- [ ] **Step 4: Commit layer BLL — thành viên B**

```bash
git config user.name "Ten Thanh Vien B"
git config user.email "email-b@example.com"
git add RagChatbot.BLL/
git commit -m "chore(BLL): import Business Logic Layer từ Assignment 2"
```

- [ ] **Step 5: Commit Presentation (Pages/Hubs) — thành viên C**

```bash
git config user.name "Ten Thanh Vien C"
git config user.email "email-c@example.com"
git add RagChatbot.RazorPages/Pages/ RagChatbot.RazorPages/Hubs/ RagChatbot.RazorPages/Program.cs
git commit -m "chore(Web): import Razor Pages + SignalR Hubs từ Assignment 2"
```

- [ ] **Step 6: Commit phần còn lại Presentation (BackgroundTasks/Services/wwwroot) — thành viên D**

```bash
git config user.name "Ten Thanh Vien D"
git config user.email "email-d@example.com"
git add RagChatbot.RazorPages/
git commit -m "chore(Web): import background worker, services, static assets"
```

- [ ] **Step 7: Verify lịch sử**

```bash
git log --oneline --pretty=format:"%an %s"
```
Expected: 5 commit, tác giả A/B/C/D phân bố đều theo layer.

> **Push:** tạo repo GitHub rồi `git remote add origin <url>` và `git push -u origin main`. (Chạy khi nhóm sẵn sàng — không tự động.)

---

### Task 3: Verify app chạy + DB migrate

**Files:** không đổi code; chỉ kiểm chứng.

**Interfaces:**
- Consumes: solution build được, PostgreSQL + pgvector đã cài (theo README A2).
- Produces: xác nhận app khởi động, DB được tạo — nền tảng cho mọi plan sau.

- [ ] **Step 1: Cấu hình secrets local**

```bash
cd "E:/Project/PRN222-FinalProject/RagChatbot.RazorPages"
# điền ConnectionStrings:DefaultConnection trong appsettings.json (đã gitignore)
dotnet user-secrets set "Gemini:ApiKey" "YOUR_GEMINI_API_KEY"
```

- [ ] **Step 2: Apply migrations (tạo DB)**

```bash
cd "E:/Project/PRN222-FinalProject"
dotnet ef database update --project RagChatbot.DAL --startup-project RagChatbot.RazorPages
```
Expected: `Done.` và DB `educhatbot` có bảng + seed 13 user.

- [ ] **Step 3: Chạy thử app**

```bash
cd "E:/Project/PRN222-FinalProject/RagChatbot.RazorPages"
dotnet run
```
Expected: log `Now listening on: http://localhost:5136`. Mở trình duyệt → login `admin`/`123` vào được Tổng quan. Ctrl+C để dừng.

- [ ] **Step 4: Không có gì để commit (chỉ verify).**

---

### Task 4: Thêm test project xUnit (hạ tầng TDD)

**Files:**
- Create: `E:/Project/PRN222-FinalProject/RagChatbot.Tests/RagChatbot.Tests.csproj`
- Create: `E:/Project/PRN222-FinalProject/RagChatbot.Tests/SemanticChunkerTests.cs`
- Modify: `PRN222-FinalProject.sln` (add test project)

**Interfaces:**
- Consumes: `RagChatbot.BLL.Helpers.SemanticChunker.SplitText(string, int, int)` (đã có sẵn từ A2).
- Produces: project test chạy `dotnet test` xanh — mọi plan sau viết test ở đây.

- [ ] **Step 1: Tạo test project + tham chiếu BLL**

```bash
cd "E:/Project/PRN222-FinalProject"
dotnet new xunit -n RagChatbot.Tests -o RagChatbot.Tests
dotnet sln add RagChatbot.Tests/RagChatbot.Tests.csproj
dotnet add RagChatbot.Tests/RagChatbot.Tests.csproj reference RagChatbot.BLL/RagChatbot.BLL.csproj
```

- [ ] **Step 2: Viết test smoke cho SemanticChunker (kiểm hạ tầng test)**

Tạo `RagChatbot.Tests/SemanticChunkerTests.cs`:

```csharp
using RagChatbot.BLL.Helpers;
using Xunit;

namespace RagChatbot.Tests;

public class SemanticChunkerTests
{
    [Fact]
    public void SplitText_EmptyInput_ReturnsEmptyList()
    {
        var result = SemanticChunker.SplitText("", maxWordsPerChunk: 400, overlapSentences: 2);
        Assert.Empty(result);
    }

    [Fact]
    public void SplitText_ShortParagraph_ReturnsSingleChunk()
    {
        var text = "Câu một. Câu hai. Câu ba.";
        var result = SemanticChunker.SplitText(text, maxWordsPerChunk: 400, overlapSentences: 2);
        Assert.Single(result);
        Assert.Contains("Câu một", result[0]);
    }

    [Fact]
    public void SplitText_ExceedsMaxWords_SplitsIntoMultipleChunks()
    {
        // 50 câu, mỗi câu ~10 từ, maxWords nhỏ để ép chia
        var sentences = string.Join(" ", Enumerable.Range(1, 50)
            .Select(i => $"Đây là câu số {i} với một ít nội dung phụ."));
        var result = SemanticChunker.SplitText(sentences, maxWordsPerChunk: 40, overlapSentences: 2);
        Assert.True(result.Count > 1, "Văn bản dài phải được chia thành nhiều chunk");
    }
}
```

- [ ] **Step 3: Chạy test — kỳ vọng PASS**

```bash
cd "E:/Project/PRN222-FinalProject"
dotnet test RagChatbot.Tests/RagChatbot.Tests.csproj
```
Expected: `Passed! - Failed: 0, Passed: 3`.

- [ ] **Step 4: Commit (thành viên B — BLL/test)**

```bash
git config user.name "Ten Thanh Vien B"
git config user.email "email-b@example.com"
git add RagChatbot.Tests/ PRN222-FinalProject.sln
git commit -m "test: thêm project xUnit + test SemanticChunker (hạ tầng TDD)"
```

---

## Self-Review

- **Spec coverage:** P0 phủ mục "Seed repo" (§8 spec) + nền tảng cho toàn bộ module. Các module M1–M7 thuộc các plan sau (P1–P6) — đã liệt kê trong index.
- **Placeholder scan:** không có TBD/TODO; mọi bước có lệnh cụ thể. Các giá trị cá nhân (tên/email thành viên, connection string, API key) là dữ liệu người dùng phải điền, không phải placeholder logic.
- **Type consistency:** chỉ dùng `SemanticChunker.SplitText(string, int, int)` — khớp chữ ký sẵn có trong A2.
