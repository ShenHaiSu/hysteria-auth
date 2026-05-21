using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HysteriaAuth.Master.Models.Entities;

public class AuthLog
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    public long? UserId { get; set; }

    [Required]
    [MaxLength(64)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MaxLength(64)]
    public string NodeId { get; set; } = string.Empty;

    [Required]
    [MaxLength(45)]
    public string ClientIp { get; set; } = string.Empty;

    public bool Success { get; set; }

    [MaxLength(256)]
    public string? Reason { get; set; }

    public DateTime AuthTime { get; set; } = DateTime.UtcNow;

    // 导航属性
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }
}
