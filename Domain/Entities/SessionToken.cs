namespace Hospital.Domain.Entities;

public class SessionToken
{
    public Guid Token { get; set; }

    public Guid UserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
}
