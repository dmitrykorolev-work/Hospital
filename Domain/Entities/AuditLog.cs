using Hospital.Domain.Enums;

namespace Hospital.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public AuditAct Action { get; set; }
    public string Details { get; set; } = null!;
}