using Hospital.Domain.Enums;

namespace Hospital.Domain.Entities;
public class User
{
    public Guid Id { get; set; }

    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;

    public Role Role { get; set; }

    public bool IsBlocked { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}