using System;
using System.Linq;
using System.Threading.Tasks;
using SecPerf.Domain.Entities;

namespace SecPerf.Domain.Repositories
{
    public interface IProductRepository
    {
        Task<Product?> GetByIdAsync(Guid id);
        Task AddAsync(Product product);
        Task UpdateAsync(Product product);
        Task DeleteAsync(Product product);
        IQueryable<Product> Query();
        IQueryable<Product> QueryByCategory(Guid categoryId);
        Task<int> SaveChangesAsync();
    }
}
