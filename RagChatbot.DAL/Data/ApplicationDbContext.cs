using RagChatbot.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace RagChatbot.DAL.Data
{
    public class ApplicationDbContext : DbContext
    {
        // Constructor nhận cấu hình từ lớp MVC truyền xuống
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // Khai báo các bảng sẽ có trong Database
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<DocumentChunk> DocumentChunks { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<UserSubject> UserSubjects { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<Quiz> Quizzes { get; set; }
        public DbSet<QuizQuestion> QuizQuestions { get; set; }
        public DbSet<QuizResult> QuizResults { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.HasPostgresExtension("vector");

            modelBuilder.Entity<User>().ToTable("User");

            modelBuilder.Entity<UserSubject>().HasKey(us => new { us.UserId, us.SubjectId });

            modelBuilder.Entity<UserSubject>()
                .HasOne(us => us.User)
                .WithMany(u => u.UserSubjects)
                .HasForeignKey(us => us.UserId);

            modelBuilder.Entity<UserSubject>()
                .HasOne(us => us.Subject)
                .WithMany(s => s.UserSubjects)
                .HasForeignKey(us => us.SubjectId);

            // Thông báo gắn với người nhận; xóa user thì xóa luôn thông báo
            modelBuilder.Entity<Notification>()
                .HasIndex(n => new { n.UserId, n.IsRead });
            modelBuilder.Entity<Notification>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Lịch sử chat truy theo (UserId, SubjectId)
            modelBuilder.Entity<ChatMessage>()
                .HasIndex(m => new { m.UserId, m.SubjectId });

            modelBuilder.Entity<Quiz>()
                .HasOne(q => q.Document)
                .WithMany()
                .HasForeignKey(q => q.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Quiz>()
                .HasOne(q => q.CreatedBy)
                .WithMany()
                .HasForeignKey(q => q.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<QuizQuestion>()
                .HasOne(q => q.Quiz)
                .WithMany(q => q.Questions)
                .HasForeignKey(q => q.QuizId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<QuizResult>()
                .HasOne(qr => qr.Quiz)
                .WithMany(q => q.Results)
                .HasForeignKey(qr => qr.QuizId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<QuizResult>()
                .HasOne(qr => qr.User)
                .WithMany()
                .HasForeignKey(qr => qr.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>().HasData(
                new User { Id = 1,  Username = "admin",       Password = "$2a$11$36oZGMR0pUm/uccAWPAXquewdW59sC4q5ZyPieDIq0OezNeL/dIVu", Role = "Admin",    FullName = "Nguyễn Quản Trị" },
                new User { Id = 2,  Username = "giangvien",   Password = "$2a$11$36oZGMR0pUm/uccAWPAXquewdW59sC4q5ZyPieDIq0OezNeL/dIVu", Role = "Lecturer", FullName = "Trần Thị Hương" },
                new User { Id = 3,  Username = "sinhvien",    Password = "$2a$11$36oZGMR0pUm/uccAWPAXquewdW59sC4q5ZyPieDIq0OezNeL/dIVu", Role = "Student",  FullName = "Lê Văn An" },
                new User { Id = 4,  Username = "gv_minh",     Password = "$2a$11$36oZGMR0pUm/uccAWPAXquewdW59sC4q5ZyPieDIq0OezNeL/dIVu", Role = "Lecturer", FullName = "Phạm Quốc Minh" },
                new User { Id = 5,  Username = "gv_lan",      Password = "$2a$11$36oZGMR0pUm/uccAWPAXquewdW59sC4q5ZyPieDIq0OezNeL/dIVu", Role = "Lecturer", FullName = "Ngô Thị Lan" },
                new User { Id = 6,  Username = "sv_bao",      Password = "$2a$11$36oZGMR0pUm/uccAWPAXquewdW59sC4q5ZyPieDIq0OezNeL/dIVu", Role = "Student",  FullName = "Đặng Châu Bảo" },
                new User { Id = 7,  Username = "sv_tung",     Password = "$2a$11$36oZGMR0pUm/uccAWPAXquewdW59sC4q5ZyPieDIq0OezNeL/dIVu", Role = "Student",  FullName = "Hoàng Minh Tùng" },
                new User { Id = 8,  Username = "sv_linh",     Password = "$2a$11$36oZGMR0pUm/uccAWPAXquewdW59sC4q5ZyPieDIq0OezNeL/dIVu", Role = "Student",  FullName = "Vũ Thị Linh" },
                new User { Id = 9,  Username = "sv_khoa",     Password = "$2a$11$36oZGMR0pUm/uccAWPAXquewdW59sC4q5ZyPieDIq0OezNeL/dIVu", Role = "Student",  FullName = "Bùi Thanh Khoa" },
                new User { Id = 10, Username = "sv_ngan",     Password = "$2a$11$36oZGMR0pUm/uccAWPAXquewdW59sC4q5ZyPieDIq0OezNeL/dIVu", Role = "Student",  FullName = "Trịnh Thị Ngân" },
                new User { Id = 11, Username = "sv_hieu",     Password = "$2a$11$36oZGMR0pUm/uccAWPAXquewdW59sC4q5ZyPieDIq0OezNeL/dIVu", Role = "Student",  FullName = "Lý Công Hiếu" },
                new User { Id = 12, Username = "sv_phuong",   Password = "$2a$11$36oZGMR0pUm/uccAWPAXquewdW59sC4q5ZyPieDIq0OezNeL/dIVu", Role = "Student",  FullName = "Dương Thị Phương" },
                new User { Id = 13, Username = "sv_duc",      Password = "$2a$11$36oZGMR0pUm/uccAWPAXquewdW59sC4q5ZyPieDIq0OezNeL/dIVu", Role = "Student",  FullName = "Mai Xuân Đức" }
            );
        }
    }
}