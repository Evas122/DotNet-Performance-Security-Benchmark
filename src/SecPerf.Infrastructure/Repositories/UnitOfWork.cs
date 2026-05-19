using System;
using System.Threading.Tasks;
using SecPerf.Domain.Repositories;
using SecPerf.Infrastructure.Data;

namespace SecPerf.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private readonly IProductRepository _productRepository;
        private readonly IUserRepository _userRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IOrderRepository _orderRepository;

        public UnitOfWork(ApplicationDbContext context,
            IProductRepository productRepository,
            IUserRepository userRepository,
            IRefreshTokenRepository refreshTokenRepository,
            ICategoryRepository categoryRepository,
            IOrderRepository orderRepository)
        {
            _context = context;
            _productRepository = productRepository;
            _userRepository = userRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _categoryRepository = categoryRepository;
            _orderRepository = orderRepository;
        }

        public IProductRepository Products => _productRepository;
        public IUserRepository Users => _userRepository;
        public IRefreshTokenRepository RefreshTokens => _refreshTokenRepository;
        public IOrderRepository Orders => _orderRepository;

        public async Task<int> CommitAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
