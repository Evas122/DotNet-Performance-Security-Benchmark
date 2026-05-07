using System;
using System.Threading.Tasks;

namespace SecPerf.Domain.Repositories
{
    public interface IUnitOfWork : IDisposable
    {
        IProductRepository Products { get; }
        IUserRepository Users { get; }
        IRefreshTokenRepository RefreshTokens { get; }

        Task<int> CommitAsync();
    }
}
