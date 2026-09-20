using System.Collections.Generic;
using System.Threading.Tasks;
using YAvuzeli.Shared.Students;

namespace YAvuzeli.Application.Services;

public interface IStudentService
{
    Task<StudentDto> GetByIdAsync(System.Guid id);
    Task<IEnumerable<StudentDto>> GetAllAsync();
    Task<StudentDto> CreateAsync(StudentInput input);
    Task<StudentDto> UpdateAsync(System.Guid id, StudentInput input);
    Task DeleteAsync(System.Guid id);
}
