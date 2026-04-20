using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SecPerf.Application.Dtos.User;
using SecPerf.Domain.Entities;
using SecPerf.Domain.Repositories;

namespace SecPerf.Application.Services.Impl
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<UserResponseDto> CreateAsync(UserRequestDto dto)
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = dto.Email,
                PasswordHash = dto.Password, // in real app hash here
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Role = dto.Role,
                CreatedAt = DateTime.UtcNow
            };

            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();

            return Map(user);
        }

        public async Task DeleteAsync(Guid id)
        {
            var u = await _userRepository.GetByIdAsync(id);
            if (u == null) return;
            // soft-delete
            u.IsDeleted = true;
            u.DeletedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(u);
            await _userRepository.SaveChangesAsync();
        }

        public async Task<IEnumerable<UserResponseDto>> GetAllAsync()
        {
            var users = _userRepository.Query().ToList();
            return users.Select(Map).ToList();
        }

        public Task<UserResponseDto?> GetByEmailAsync(string email)
        {
            return Task.FromResult(_userRepository.Query().Where(u => u.Email == email).Select(u => Map(u)).FirstOrDefault());
        }

        public async Task<UserResponseDto?> GetByIdAsync(Guid id)
        {
            var u = await _userRepository.GetByIdAsync(id);
            return u == null ? null : Map(u);
        }

        public async Task UpdateAsync(Guid id, UserRequestDto dto)
        {
            var u = await _userRepository.GetByIdAsync(id);
            if (u == null) return;
            u.Email = dto.Email;
            u.FirstName = dto.FirstName;
            u.LastName = dto.LastName;
            if (!string.IsNullOrEmpty(dto.Password))
            {
                u.PasswordHash = dto.Password; // hash in real app
            }
            u.Role = dto.Role;
            await _userRepository.UpdateAsync(u);
            await _userRepository.SaveChangesAsync();
        }

        private static UserResponseDto Map(User u)
        {
            return new UserResponseDto
            {
                Id = u.Id,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Role = u.Role,
                CreatedAt = u.CreatedAt
            };
        }
    }
}
