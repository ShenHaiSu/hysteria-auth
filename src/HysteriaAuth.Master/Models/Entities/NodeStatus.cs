using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HysteriaAuth.Master.Models.Entities;

public class NodeStatus
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Required]
    [MaxLength(64)]
    public string NodeId { get; set; } = string.Empty;

    public float CpuUsagePercent { get; set; }

    public float MemoryUsagePercent { get; set; }

    public float MemoryUsedMb { get; set; }

    public float MemoryTotalMb { get; set; }

    public long NetworkInBytes { get; set; }

    public long NetworkOutBytes { get; set; }

    public float NetworkInMbps { get; set; }

    public float NetworkOutMbps { get; set; }

    public int ActiveConnections { get; set; }

    public DateTime ReportedAt { get; set; } = DateTime.UtcNow;

    // 导航属性
    [ForeignKey(nameof(NodeId))]
    public Node? Node { get; set; }
}
