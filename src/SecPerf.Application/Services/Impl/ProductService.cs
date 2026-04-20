using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SecPerf.Application.Dtos.Product;
using SecPerf.Domain.Entities;
using SecPerf.Domain.Repositories;

namespace SecPerf.Application.Services.Impl
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;

        public ProductService(IProductRepository productRepository, ICategoryRepository categoryRepository)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
        }

        public async Task<ProductResponseDto> CreateAsync(ProductRequestDto dto)
        {
            // ensure category exists
            var cat = await _categoryRepository.GetByIdAsync(dto.CategoryId);
            if (cat == null) throw new InvalidOperationException("Category not found");

            var p = new Product
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                Stock = dto.Stock,
                CategoryId = dto.CategoryId
            };

            await _productRepository.AddAsync(p);
            await _productRepository.SaveChangesAsync();
            return Map(p);
        }

        public Task DeleteAsync(Guid id)
        {
            var p = _productRepository.Query().FirstOrDefault(x => x.Id == id);
            if (p == null) return Task.CompletedTask;
            // soft-delete
            p.IsDeleted = true;
            p.DeletedAt = DateTime.UtcNow;
            _productRepository.UpdateAsync(p);
            return _productRepository.SaveChangesAsync();
        }

        public Task<IEnumerable<ProductResponseDto>> GetAllAsync(Guid? categoryId = null)
        {
            var q = _productRepository.Query();
            if (categoryId.HasValue) q = q.Where(x => x.CategoryId == categoryId.Value);
            var list = q.Select(Map).ToList();
            return Task.FromResult((IEnumerable<ProductResponseDto>)list);
        }

        public async Task<ProductResponseDto?> GetByIdAsync(Guid id)
        {
            var p = await _productRepository.GetByIdAsync(id);
            return p == null ? null : Map(p);
        }

        public async Task UpdateAsync(Guid id, ProductRequestDto dto)
        {
            var p = await _productRepository.GetByIdAsync(id);
            if (p == null) return;
            p.Name = dto.Name;
            p.Description = dto.Description;
            p.Price = dto.Price;
            p.Stock = dto.Stock;
            p.CategoryId = dto.CategoryId;
            await _productRepository.UpdateAsync(p);
            await _productRepository.SaveChangesAsync();
        }

        private static ProductResponseDto Map(Product p)
        {
            return new ProductResponseDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                Stock = p.Stock,
                CategoryId = p.CategoryId
            };
        }
    }
}
