using System;

namespace YAvuzeli.Application.DTOs;

public record StudentDto(
    Guid Id,
    string FirstName,
    string LastName,
    string? Email,
    string? Phone,
    DateTime DateOfBirth,
    bool IsActive,
    DateTime CreatedAt);
