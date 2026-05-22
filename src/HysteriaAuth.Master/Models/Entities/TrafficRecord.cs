using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HysteriaAuth.Master.Models.Entities;

public class TrafficRecord
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    public long UserId { get; set; }

    /// <summary>
    /// 入站流量/用户上传（字节）。对应 Hysteria rx。
    /// </summary>
    public long BytesIn { get; set; }

    /// <summary>
    /// 出站流量/用户下载（字节）。对应 Hysteria tx。
    /// </summary>
    public long BytesOut { get; set; }

    [Required]
    [MaxLength(64)]
    public string NodeId { get; set; } = string.Empty;

    /// <summary>
    /// 幂等键，格式 {nodeId}_{username}_{timestamp_rounded}，防止重复计入。
    /// </summary>
    [Required]
    [MaxLength(128)]
    public string IdempotencyKey { get; set; } = string.Empty;

    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    // 导航属性
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }
}
