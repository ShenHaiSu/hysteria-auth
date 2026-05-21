using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HysteriaAuth.Master.Models.Entities;

public class Node
{
    [Key]
    [MaxLength(64)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [MaxLength(128)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(45)]
    public string IpAddress { get; set; } = string.Empty;

    public int Port { get; set; }

    [Required]
    [MaxLength(256)]
    public string SecretKey { get; set; } = string.Empty;

    public int SecretVersion { get; set; } = 1;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastHeartbeat { get; set; }

    [MaxLength(128)]
    public string? Location { get; set; }

    public int? TrafficStatsPort { get; set; }

    [MaxLength(256)]
    public string? TrafficStatsSecret { get; set; }

    [MaxLength(128)]
    public string? ProvisionToken { get; set; }

    [Required]
    [MaxLength(16)]
    public string ProvisionStatus { get; set; } = "pending";
}
