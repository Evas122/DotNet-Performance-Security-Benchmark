using System;
using System.Linq;
using System.Threading.Tasks;
using SecPerf.Domain.Entities;

namespace SecPerf.Domain.Repositories
{
    public interface ICategoryRepository
    {
        Task<Category?> GetByIdAsync(Guid id);
        Task AddAsync(Category category);
        Task UpdateAsync(Category category);
        Task DeleteAsync(Category category);
        IQueryable<Category> Query();
        Task<int> SaveChangesAsync();
    }
}
