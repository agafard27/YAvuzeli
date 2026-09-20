namespace YAvuzeli.Shared.Students;

public sealed record StudentDto(Guid Id, string FirstName, string LastName,
    string? Email, string? Phone, DateOnly DateOfBirth, bool IsActive, DateTime CreatedAt);
