using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HysteriaAuth.Master.Models.Entities;

public class AdminAuditLog
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    public long AdminId { get; set; }

    [Required]
    [MaxLength(64)]
    public string Action { get; set; } = string.Empty;

    [Required]
    [MaxLength(32)]
    public string TargetType { get; set; } = string.Empty;

    [MaxLength(64)]
    public string? TargetId { get; set; }

    public string? Detail { get; set; }

    [Required]
    [MaxLength(45)]
    public string ClientIp { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // 导航属性
    [ForeignKey(nameof(AdminId))]
    public Admin? Admin { get; set; }
}
