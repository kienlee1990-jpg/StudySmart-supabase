using QuanLyTaiKhoanAPI.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;

namespace QuanLyTaiKhoanAPI.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>, IDataProtectionKeyContext
{
    public const string Schema = "hoctap";
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;

    // ================= User Management =================
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    // ================= RBAC =================
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<UserPermission> UserPermissions { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<QuizExam> QuizExams => Set<QuizExam>();
    public DbSet<QuizQuestion> QuizQuestions => Set<QuizQuestion>();
    public DbSet<QuizOption> QuizOptions => Set<QuizOption>();
    public DbSet<ExamQuestion> ExamQuestions => Set<ExamQuestion>();
    public DbSet<QuizAttempt> QuizAttempts => Set<QuizAttempt>();
    public DbSet<QuizAnswer> QuizAnswers => Set<QuizAnswer>();
    public DbSet<QuizLesson> QuizLessons => Set<QuizLesson>();
    public DbSet<TowerRank> TowerRanks => Set<TowerRank>();
    public DbSet<TowerProgress> TowerProgress => Set<TowerProgress>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema(Schema);

        // =====================================================
        // RefreshToken
        // =====================================================
        builder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.TokenHash)
                  .IsRequired()
                  .HasMaxLength(128);

            entity.Property(x => x.ReplacedByTokenHash)
                  .HasMaxLength(128);

            entity.HasIndex(x => x.TokenHash)
                  .IsUnique();

            entity.HasOne(x => x.User)
                  .WithMany(u => u.RefreshTokens)
                  .HasForeignKey(x => x.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // =====================================================
        // Permission
        // =====================================================
        builder.Entity<Permission>(entity =>
        {
            entity.HasKey(p => p.Id);

            entity.Property(p => p.Name)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.HasIndex(p => p.Name)
                  .IsUnique();
        });

        // =====================================================
        // RolePermission
        // =====================================================
        builder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(rp => new { rp.RoleId, rp.PermissionId });

            entity.HasOne(rp => rp.Role)
                  .WithMany()
                  .HasForeignKey(rp => rp.RoleId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(rp => rp.Permission)
                  .WithMany()
                  .HasForeignKey(rp => rp.PermissionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // =====================================================
        // UserPermission
        // =====================================================
        builder.Entity<UserPermission>(entity =>
        {
            entity.HasKey(up => new { up.UserId, up.PermissionId });

            entity.Property(up => up.IsGranted)
                  .IsRequired();

            entity.HasOne(up => up.User)
                  .WithMany(u => u.UserPermissions)
                  .HasForeignKey(up => up.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(up => up.Permission)
                  .WithMany()
                  .HasForeignKey(up => up.PermissionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(auditLog => auditLog.Id);

            entity.Property(auditLog => auditLog.Action)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(auditLog => auditLog.ActorUserId)
                  .HasMaxLength(450);

            entity.Property(auditLog => auditLog.ActorUserName)
                  .HasMaxLength(256);

            entity.Property(auditLog => auditLog.TargetUserId)
                  .HasMaxLength(450);

            entity.Property(auditLog => auditLog.TargetUserName)
                  .HasMaxLength(256);

            entity.Property(auditLog => auditLog.EntityName)
                  .HasMaxLength(100);

            entity.Property(auditLog => auditLog.EntityId)
                  .HasMaxLength(450);

            entity.Property(auditLog => auditLog.Details)
                  .HasMaxLength(2000);

            entity.Property(auditLog => auditLog.IpAddress)
                  .HasMaxLength(64);

            entity.Property(auditLog => auditLog.UserAgent)
                  .HasMaxLength(512);

            entity.HasIndex(auditLog => auditLog.CreatedAt);
            entity.HasIndex(auditLog => auditLog.Action);
            entity.HasIndex(auditLog => auditLog.ActorUserId);
            entity.HasIndex(auditLog => auditLog.TargetUserId);
        });

        builder.Entity<ExamQuestion>(entity =>
        {
            entity.HasKey(item => new { item.ExamId, item.QuestionId });
            entity.HasOne(item => item.Exam).WithMany(item => item.Questions)
                .HasForeignKey(item => item.ExamId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.Question).WithMany(item => item.Exams)
                .HasForeignKey(item => item.QuestionId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<QuizOption>()
            .HasOne(item => item.Question).WithMany(item => item.Options)
            .HasForeignKey(item => item.QuestionId).OnDelete(DeleteBehavior.Cascade);

        builder.Entity<QuizAttempt>(entity =>
        {
            entity.Property(item => item.Score).HasPrecision(5, 2);
            entity.Property(item => item.SubmittedAt).IsConcurrencyToken();
            entity.Property(item => item.GuestId).HasMaxLength(64);
            entity.HasOne(item => item.Exam).WithMany(item => item.Attempts)
                .HasForeignKey(item => item.ExamId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.User).WithMany()
                .HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<TowerRank>(entity =>
        {
            entity.HasKey(item => new { item.UserId, item.Subject });
            entity.Property(item => item.Subject).HasMaxLength(30);
            entity.Property(item => item.Revision).IsConcurrencyToken();
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(item => item.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<TowerProgress>(entity =>
        {
            entity.Property(item => item.GuestId).HasMaxLength(64);
            entity.Property(item => item.Subject).HasMaxLength(30).IsRequired();
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(item => item.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(item => new { item.UserId, item.Subject, item.Level }).IsUnique()
                .HasFilter("\"UserId\" IS NOT NULL");
            entity.HasIndex(item => new { item.GuestId, item.Subject, item.Level }).IsUnique()
                .HasFilter("\"GuestId\" IS NOT NULL");
        });

        builder.Entity<QuizAnswer>(entity =>
        {
            entity.HasIndex(item => new { item.AttemptId, item.QuestionId }).IsUnique();
            entity.HasOne(item => item.Attempt).WithMany(item => item.Answers)
                .HasForeignKey(item => item.AttemptId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.Question).WithMany()
                .HasForeignKey(item => item.QuestionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.Option).WithMany()
                .HasForeignKey(item => item.OptionId).OnDelete(DeleteBehavior.SetNull);
        });
    }
}
