using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SecPerf.Application.Dtos.Product;

namespace SecPerf.Application.Services
{
    public interface IProductService
    {
        Task<ProductResponseDto?> GetByIdAsync(Guid id);
        Task<IEnumerable<ProductResponseDto>> GetAllAsync(Guid? categoryId = null);
        Task<ProductResponseDto> CreateAsync(ProductRequestDto dto);
        Task UpdateAsync(Guid id, ProductRequestDto dto);
        Task DeleteAsync(Guid id);
    }
}
