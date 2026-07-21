using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using RagChatbot.BLL.Extensions;
using RagChatbot.BLL.Services.Interfaces;
using RagChatbot.RazorPages.BackgroundTasks;
using RagChatbot.RazorPages.Hubs;
using RagChatbot.RazorPages.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddSignalR();

// Hàng đợi + worker xử lý tài liệu chạy nền (đẩy trạng thái real-time qua SignalR)
builder.Services.AddSingleton<IDocumentProcessingQueue, DocumentProcessingQueue>();
builder.Services.AddHostedService<DocumentProcessingWorker>();

// Dịch vụ gửi thông báo: lưu DB (BLL) + đẩy real-time (SignalR)
builder.Services.AddScoped<IRealtimeNotifier, RealtimeNotifier>();

// Dashboard sống: theo dõi online (singleton) + báo đổi số liệu
builder.Services.AddSingleton<IPresenceTracker, PresenceTracker>();
builder.Services.AddScoped<IDashboardNotifier, DashboardNotifier>();

// Gọi hàm Extension từ tầng BLL để đăng ký toàn bộ Dependency Injection
builder.Services.AddProjectDependencies(builder.Configuration);
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login"; // Đường dẫn bị đẩy về nếu chưa đăng nhập
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(2); // Giữ đăng nhập trong 2 tiếng

        // Mỗi request: đối chiếu vai trò trong cookie với DB. Nếu Admin đã đổi quyền
        // hoặc xóa tài khoản → từ chối phiên và đăng xuất (buộc đăng nhập lại).
        options.Events = new CookieAuthenticationEvents
        {
            OnValidatePrincipal = async context =>
            {
                var principal = context.Principal;
                if (principal?.Identity?.IsAuthenticated != true) return;

                var username = principal.FindFirstValue(ClaimTypes.Name);
                var roleInCookie = principal.FindFirstValue(ClaimTypes.Role);
                if (string.IsNullOrEmpty(username)) return;

                var userService = context.HttpContext.RequestServices.GetRequiredService<IUserService>();
                var current = userService.GetByUsername(username);

                if (current == null || current.Role != roleInCookie)
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                }
            }
        };
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

// Phục vụ file tĩnh runtime (tài liệu upload vào wwwroot/uploads) — MapStaticAssets bên dưới
// chỉ phục vụ asset đã biết lúc build, không thấy các file được ghi vào wwwroot sau khi chạy.
app.UseStaticFiles();

app.MapStaticAssets();

// Trang gốc "/" → điều hướng về Tổng quan (sẽ tự đẩy về Login nếu chưa đăng nhập)
app.MapGet("/", () => Results.Redirect("/Home/Index"));

app.MapRazorPages()
    .WithStaticAssets();

// Hub real-time cho Chat AI
app.MapHub<ChatHub>("/chatHub");

// Hub real-time cho trạng thái xử lý tài liệu
app.MapHub<DocumentHub>("/documentHub");

// Hub thông báo chung tới từng người dùng
app.MapHub<NotificationHub>("/notificationHub");

// Hub cập nhật danh sách (thành viên / môn học) real-time
app.MapHub<SubjectHub>("/subjectHub");

// Hub dashboard sống (presence + broadcast)
app.MapHub<DashboardHub>("/dashboardHub");

// Hub cập nhật danh sách tài khoản real-time
app.MapHub<UserHub>("/userHub");

app.Run();
