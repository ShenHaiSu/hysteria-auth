using Microsoft.EntityFrameworkCore;
using HysteriaAuth.Master.Models.Entities;

namespace HysteriaAuth.Master.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Node> Nodes => Set<Node>();
    public DbSet<TrafficRecord> TrafficRecords => Set<TrafficRecord>();
    public DbSet<AuthLog> AuthLogs => Set<AuthLog>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<NodeStatus> NodeStatuses => Set<NodeStatus>();
    public DbSet<NodeTraffic> NodeTraffics => Set<NodeTraffic>();
    public DbSet<Admin> Admins => Set<Admin>();
    public DbSet<AdminAuditLog> AdminAuditLogs => Set<AdminAuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ============================
        // User 表
        // ============================
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasIndex(u => u.Username).IsUnique();
            entity.Property(u => u.RowVersion).IsRowVersion();
        });

        // ============================
        // Node 表
        // ============================
        modelBuilder.Entity<Node>(entity =>
        {
            entity.ToTable("Nodes");
            entity.HasIndex(n => n.ProvisionToken).IsUnique();
        });

        // ============================
        // TrafficRecord 表
        // ============================
        modelBuilder.Entity<TrafficRecord>(entity =>
        {
            entity.ToTable("TrafficRecords");
            entity.HasIndex(tr => tr.IdempotencyKey).IsUnique();
            entity.HasOne(tr => tr.User)
                  .WithMany()
                  .HasForeignKey(tr => tr.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ============================
        // AuthLog 表
        // ============================
        modelBuilder.Entity<AuthLog>(entity =>
        {
            entity.ToTable("AuthLogs");
            entity.HasOne(al => al.User)
                  .WithMany()
                  .HasForeignKey(al => al.UserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // ============================
        // Session 表
        // ============================
        modelBuilder.Entity<Session>(entity =>
        {
            entity.ToTable("Sessions");
            entity.HasIndex(s => s.SessionKey).IsUnique();
            entity.HasOne(s => s.User)
                  .WithMany()
                  .HasForeignKey(s => s.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ============================
        // NodeStatus 表
        // ============================
        modelBuilder.Entity<NodeStatus>(entity =>
        {
            entity.ToTable("NodeStatuses");
            entity.HasOne(ns => ns.Node)
                  .WithMany()
                  .HasForeignKey(ns => ns.NodeId)
                  .HasPrincipalKey(n => n.Id)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ============================
        // NodeTraffic 表
        // ============================
        modelBuilder.Entity<NodeTraffic>(entity =>
        {
            entity.ToTable("NodeTraffics");
            entity.HasOne(nt => nt.Node)
                  .WithMany()
                  .HasForeignKey(nt => nt.NodeId)
                  .HasPrincipalKey(n => n.Id)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ============================
        // Admin 表
        // ============================
        modelBuilder.Entity<Admin>(entity =>
        {
            entity.ToTable("Admins");
            entity.HasIndex(a => a.Username).IsUnique();
        });

        // ============================
        // AdminAuditLog 表（只读，审计日志不可修改/删除）
        // ============================
        modelBuilder.Entity<AdminAuditLog>(entity =>
        {
            entity.ToTable("AdminAuditLogs");
            entity.HasOne(aal => aal.Admin)
                  .WithMany()
                  .HasForeignKey(aal => aal.AdminId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }

    /// <summary>
    /// 禁用对 AdminAuditLogs 的 Update 和 Delete 操作，确保审计日志不可篡改。
    /// </summary>
    public override int SaveChanges()
    {
        // 检查是否有对 AdminAuditLogs 的修改或删除操作
        var auditLogEntries = ChangeTracker.Entries<AdminAuditLog>()
            .Where(e => e.State == EntityState.Modified || e.State == EntityState.Deleted);

        if (auditLogEntries.Any())
        {
            throw new InvalidOperationException("AdminAuditLogs 记录不可修改或删除。审计日志一旦写入即不可变更。");
        }

        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var auditLogEntries = ChangeTracker.Entries<AdminAuditLog>()
            .Where(e => e.State == EntityState.Modified || e.State == EntityState.Deleted);

        if (auditLogEntries.Any())
        {
            throw new InvalidOperationException("AdminAuditLogs 记录不可修改或删除。审计日志一旦写入即不可变更。");
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
