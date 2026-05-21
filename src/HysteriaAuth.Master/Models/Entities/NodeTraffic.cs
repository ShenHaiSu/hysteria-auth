using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HysteriaAuth.Master.Models.Entities;

public class NodeTraffic
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Required]
    [MaxLength(64)]
    public string NodeId { get; set; } = string.Empty;

    public long TotalBytesIn { get; set; }

    public long TotalBytesOut { get; set; }

    public int ActiveUsers { get; set; }

    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    // 导航属性
    [ForeignKey(nameof(NodeId))]
    public Node? Node { get; set; }
}
