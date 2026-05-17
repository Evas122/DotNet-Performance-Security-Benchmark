using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SecPerf.Domain.Entities;
using SecPerf.Domain.Repositories;
using SecPerf.Infrastructure.Data;

namespace SecPerf.Infrastructure.Repositories
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly ApplicationDbContext _context;

        public RefreshTokenRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(RefreshToken token)
        {
            await _context.RefreshTokens.AddAsync(token);
        }

        public Task DeleteAsync(RefreshToken token)
        {
            _context.RefreshTokens.Remove(token);
            return Task.CompletedTask;
        }

        public Task<RefreshToken?> GetByIdAsync(Guid id)
            => _context.RefreshTokens.FirstOrDefaultAsync(t => t.Id == id);

        public Task<RefreshToken?> GetByTokenAsync(string token)
            => _context.RefreshTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Token == token);

        public Task UpdateAsync(RefreshToken token)
        {
            _context.RefreshTokens.Update(token);
            return Task.CompletedTask;
        }

        public IQueryable<RefreshToken> QueryByUser(Guid userId)
            => _context.RefreshTokens.Where(t => t.UserId == userId).AsQueryable();

        public Task<int> SaveChangesAsync()
            => _context.SaveChangesAsync();
    }
}
