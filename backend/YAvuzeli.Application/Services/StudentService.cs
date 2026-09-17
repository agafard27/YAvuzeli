using YAvuzeli.Application.DTOs;
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

    public async Task<StudentDto> CreateAsync(StudentDto input)
    {
        var student = new Student
        {
            Id = input.Id,
            FirstName = input.FirstName,
            LastName = input.LastName,
            Phone = input.Phone,
            Email = input.Email,
            DateOfBirth = input.DateOfBirth,
            IsActive = input.IsActive,
            CreatedAt = input.CreatedAt
        };

        var created = await _repository.AddAsync(student);
        return MapToDto(created);
    }

    public async Task<StudentDto> UpdateAsync(StudentDto input)
    {
        var existing = await _repository.GetByIdAsync(input.Id)
            ?? throw new KeyNotFoundException($"Student {input.Id} not found");

        existing.FirstName = input.FirstName;
        existing.LastName = input.LastName;
        existing.Phone = input.Phone;
        existing.Email = input.Email;
        existing.DateOfBirth = input.DateOfBirth;
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
            student.DateOfBirth,
            student.IsActive,
            student.CreatedAt);
    }
}
