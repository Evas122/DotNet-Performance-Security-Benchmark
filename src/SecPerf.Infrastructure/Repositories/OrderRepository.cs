using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SecPerf.Domain.Entities;
using SecPerf.Domain.Repositories;
using SecPerf.Infrastructure.Data;

namespace SecPerf.Infrastructure.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly ApplicationDbContext _context;

        public OrderRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Order order)
        {
            await _context.Orders.AddAsync(order);
        }

        public Task DeleteAsync(Order order)
        {
            _context.Orders.Remove(order);
            return Task.CompletedTask;
        }

        public Task<Order?> GetByIdAsync(Guid id)
            => _context.Orders.Include(o => o.OrderItems).FirstOrDefaultAsync(o => o.Id == id);

        public Task UpdateAsync(Order order)
        {
            _context.Orders.Update(order);
            return Task.CompletedTask;
        }

        public IQueryable<Order> Query()
            => _context.Orders.AsQueryable();

        public IQueryable<Order> QueryByUser(Guid userId)
            => _context.Orders.Where(o => o.UserId == userId).AsQueryable();

        public Task<int> SaveChangesAsync()
            => _context.SaveChangesAsync();
    }
}
