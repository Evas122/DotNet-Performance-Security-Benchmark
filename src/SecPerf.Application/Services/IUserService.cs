using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SecPerf.Application.Dtos.User;

namespace SecPerf.Application.Services
{
    public interface IUserService
    {
        Task<UserResponseDto?> GetByIdAsync(Guid id);
        Task<UserResponseDto?> GetByEmailAsync(string email);
        Task<UserResponseDto> CreateAsync(UserRequestDto dto);
        Task UpdateAsync(Guid id, UserRequestDto dto);
        Task DeleteAsync(Guid id);
        Task<IEnumerable<UserResponseDto>> GetAllAsync();
    }
}
