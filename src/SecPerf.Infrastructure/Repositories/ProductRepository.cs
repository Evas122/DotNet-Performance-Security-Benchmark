using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SecPerf.Domain.Entities;
using SecPerf.Domain.Repositories;
using SecPerf.Infrastructure.Data;

namespace SecPerf.Infrastructure.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly ApplicationDbContext _context;

        public ProductRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Product product)
        {
            await _context.Products.AddAsync(product);
        }

        public Task DeleteAsync(Product product)
        {
            _context.Products.Remove(product);
            return Task.CompletedTask;
        }

        public Task<Product?> GetByIdAsync(Guid id)
            => _context.Products.FirstOrDefaultAsync(p => p.Id == id);

        public Task UpdateAsync(Product product)
        {
            _context.Products.Update(product);
            return Task.CompletedTask;
        }

        public IQueryable<Product> Query()
            => _context.Products.AsQueryable();

        public IQueryable<Product> QueryByCategory(Guid categoryId)
            => _context.Products.Where(p => p.CategoryId == categoryId).AsQueryable();

        public Task<int> SaveChangesAsync()
            => _context.SaveChangesAsync();
    }
}
