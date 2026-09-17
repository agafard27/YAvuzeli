using YAvuzeli.Domain.Entities;

namespace YAvuzeli.Application.Repositories;

public interface IStudentRepository
{
    Task<Student?> GetByIdAsync(Guid id);
    Task<IEnumerable<Student>> GetAllAsync();
    Task<Student> AddAsync(Student student);
    Task<Student> UpdateAsync(Student student);
    Task DeleteAsync(Guid id);
}
