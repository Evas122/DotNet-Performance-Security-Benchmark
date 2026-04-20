using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SecPerf.Domain.Entities;
using SecPerf.Domain.Repositories;
using SecPerf.Infrastructure.Data;

namespace SecPerf.Infrastructure.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly ApplicationDbContext _context;

        public CategoryRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Category category)
        {
            await _context.Categories.AddAsync(category);
        }

        public Task DeleteAsync(Category category)
        {
            _context.Categories.Remove(category);
            return Task.CompletedTask;
        }

        public Task<Category?> GetByIdAsync(Guid id)
            => _context.Categories.FirstOrDefaultAsync(c => c.Id == id);

        public Task UpdateAsync(Category category)
        {
            _context.Categories.Update(category);
            return Task.CompletedTask;
        }

        public IQueryable<Category> Query()
            => _context.Categories.AsQueryable();

        public Task<int> SaveChangesAsync()
            => _context.SaveChangesAsync();
    }
}
