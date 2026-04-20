using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SecPerf.Application.Dtos.Category;

namespace SecPerf.Application.Services
{
    public interface ICategoryService
    {
        Task<CategoryResponseDto?> GetByIdAsync(Guid id);
        Task<IEnumerable<CategoryResponseDto>> GetAllAsync();
        Task<CategoryResponseDto> CreateAsync(CategoryRequestDto dto);
        Task UpdateAsync(Guid id, CategoryRequestDto dto);
        Task DeleteAsync(Guid id);
    }
}
