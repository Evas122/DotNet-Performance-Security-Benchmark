using System;
using System.Linq;
using System.Threading.Tasks;
using SecPerf.Domain.Entities;

namespace SecPerf.Domain.Repositories
{
    public interface IOrderRepository
    {
        Task<Order?> GetByIdAsync(Guid id);
        Task AddAsync(Order order);
        Task UpdateAsync(Order order);
        Task DeleteAsync(Order order);
        IQueryable<Order> Query();
        IQueryable<Order> QueryByUser(Guid userId);
        Task<int> SaveChangesAsync();
    }
}
