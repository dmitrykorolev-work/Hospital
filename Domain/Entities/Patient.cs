namespace Hospital.Domain.Entities;

public class Patient
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;

    public DateTime BirthDate { get; set; }

    public string Phone { get; set; } = null!;
    public string Email { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
