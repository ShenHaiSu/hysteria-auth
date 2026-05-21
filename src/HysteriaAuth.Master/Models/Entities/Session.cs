using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HysteriaAuth.Master.Models.Entities;

public class Session
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    public long UserId { get; set; }

    [Required]
    [MaxLength(64)]
    public string NodeId { get; set; } = string.Empty;

    [Required]
    [MaxLength(128)]
    public string SessionKey { get; set; } = string.Empty;

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public DateTime? EndedAt { get; set; }

    public long BytesIn { get; set; }

    public long BytesOut { get; set; }

    [Required]
    [MaxLength(16)]
    public string Status { get; set; } = "active";

    // 导航属性
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }
}
