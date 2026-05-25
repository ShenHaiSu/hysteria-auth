using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HysteriaAuth.Master.Models.Entities;

public class User
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Required]
    [MaxLength(64)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string Password { get; set; } = string.Empty;

    [MaxLength(128)]
    public string? Email { get; set; }

    public long TotalTrafficBytes { get; set; }

    public long UsedTrafficBytes { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// 允许的节点ID列表（JSON数组，NULL=全部节点）
    /// </summary>
    public string? AllowedNodes { get; set; }

    public string? Remark { get; set; }

    /// <summary>
    /// 并发令牌（SQLite 不支持 ROWVERSION，使用 Guid 模拟）。
    /// 每次 INSERT/UPDATE 时由 AppDbContext.SaveChangesAsync 自动生成新值。
    /// </summary>
    public Guid RowVersion { get; set; } = Guid.NewGuid();
}
