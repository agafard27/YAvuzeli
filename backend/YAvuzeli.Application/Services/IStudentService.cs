using System.Collections.Generic;
using System.Threading.Tasks;
using YAvuzeli.Application.DTOs;

namespace YAvuzeli.Application.Services;

public interface IStudentService
{
    Task<StudentDto> GetByIdAsync(System.Guid id);
    Task<IEnumerable<StudentDto>> GetAllAsync();
    Task<StudentDto> CreateAsync(StudentDto input);
    Task<StudentDto> UpdateAsync(StudentDto input);
    Task DeleteAsync(System.Guid id);
}
