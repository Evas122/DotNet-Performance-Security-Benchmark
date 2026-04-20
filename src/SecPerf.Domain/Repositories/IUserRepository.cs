using System;
using System.Linq;
using System.Threading.Tasks;
using SecPerf.Domain.Entities;

namespace SecPerf.Domain.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(Guid id);
        Task<User?> GetByEmailAsync(string email);
        Task AddAsync(User user);
        Task UpdateAsync(User user);
        Task DeleteAsync(User user);
        IQueryable<User> Query();
        Task<int> SaveChangesAsync();
    }
}
