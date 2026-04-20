using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SecPerf.Application.Dtos.Category;
using SecPerf.Domain.Entities;
using SecPerf.Domain.Repositories;

namespace SecPerf.Application.Services.Impl
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _categoryRepository;

        public CategoryService(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task<CategoryResponseDto> CreateAsync(CategoryRequestDto dto)
        {
            var c = new Category
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Description = dto.Description
            };
            await _categoryRepository.AddAsync(c);
            await _categoryRepository.SaveChangesAsync();
            return Map(c);
        }

        public Task DeleteAsync(Guid id)
        {
            var c = _categoryRepository.Query().FirstOrDefault(x => x.Id == id);
            if (c == null) return Task.CompletedTask;
            _categoryRepository.DeleteAsync(c);
            return _categoryRepository.SaveChangesAsync();
        }

        public Task<IEnumerable<CategoryResponseDto>> GetAllAsync()
        {
            var list = _categoryRepository.Query().Select(Map).ToList();
            return Task.FromResult((IEnumerable<CategoryResponseDto>)list);
        }

        public async Task<CategoryResponseDto?> GetByIdAsync(Guid id)
        {
            var c = await _categoryRepository.GetByIdAsync(id);
            return c == null ? null : Map(c);
        }

        public async Task UpdateAsync(Guid id, CategoryRequestDto dto)
        {
            var c = await _categoryRepository.GetByIdAsync(id);
            if (c == null) return;
            c.Name = dto.Name;
            c.Description = dto.Description;
            await _categoryRepository.UpdateAsync(c);
            await _categoryRepository.SaveChangesAsync();
        }

        private static CategoryResponseDto Map(Category c)
        {
            return new CategoryResponseDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description
            };
        }
    }
}
