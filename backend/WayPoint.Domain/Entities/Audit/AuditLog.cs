using System.Security.Cryptography;
using System.Text;
using WayPoint.Domain.Common;

namespace WayPoint.Domain.Entities.Audit;

public class AuditLog : BaseEntity
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string ActorId { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string? BeforeStateJson { get; set; }
    public string? AfterStateJson { get; set; }
    public string HashSha256 { get; set; } = string.Empty;

    /// <summary>
    /// Computes and sets the SHA-256 hash from the audit log's immutable fields (BR-AUDIT-001).
    /// </summary>
    public void ComputeHash()
    {
        var payload = $"{Timestamp:O}|{ActorId}|{ActionType}|{EntityName}|{EntityId}|{AfterStateJson ?? string.Empty}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        HashSha256 = Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
