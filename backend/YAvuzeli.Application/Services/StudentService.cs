using System.ComponentModel.DataAnnotations;
using YAvuzeli.Shared.Students;
using YAvuzeli.Application.Repositories;
using YAvuzeli.Application.Services;
using YAvuzeli.Domain.Entities;

namespace YAvuzeli.Application.Services;

public class StudentService : IStudentService
{
    private readonly IStudentRepository _repository;

    public StudentService(IStudentRepository repository)
    {
        _repository = repository;
    }

    public async Task<StudentDto> GetByIdAsync(Guid id)
    {
        var student = await _repository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Student {id} not found");

        return MapToDto(student);
    }

    public async Task<IEnumerable<StudentDto>> GetAllAsync()
    {
        var students = await _repository.GetAllAsync();
        return students.Select(MapToDto);
    }

    public async Task<StudentDto> CreateAsync(StudentInput input)
    {
        Validator.ValidateObject(input, new ValidationContext(input), true);
        var student = new Student
        {
            FirstName = input.FirstName.Trim(),
            LastName = input.LastName.Trim(),
            Phone = NormalizeOptional(input.Phone),
            Email = NormalizeOptional(input.Email),
            DateOfBirth = input.DateOfBirth!.Value.ToDateTime(TimeOnly.MinValue),
            IsActive = input.IsActive
        };

        var created = await _repository.AddAsync(student);
        return MapToDto(created);
    }

    public async Task<StudentDto> UpdateAsync(Guid id, StudentInput input)
    {
        Validator.ValidateObject(input, new ValidationContext(input), true);
        var existing = await _repository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Student {id} not found");

        existing.FirstName = input.FirstName.Trim();
        existing.LastName = input.LastName.Trim();
        existing.Phone = NormalizeOptional(input.Phone);
        existing.Email = NormalizeOptional(input.Email);
        existing.DateOfBirth = input.DateOfBirth!.Value.ToDateTime(TimeOnly.MinValue);
        existing.IsActive = input.IsActive;

        var updated = await _repository.UpdateAsync(existing);
        return MapToDto(updated);
    }

    public async Task DeleteAsync(Guid id)
    {
        await _repository.DeleteAsync(id);
    }

    private static StudentDto MapToDto(Student student)
    {
        return new StudentDto(
            student.Id,
            student.FirstName,
            student.LastName,
            student.Email,
            student.Phone,
            DateOnly.FromDateTime(student.DateOfBirth),
            student.IsActive,
            student.CreatedAt);
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
