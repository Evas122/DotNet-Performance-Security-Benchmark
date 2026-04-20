using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SecPerf.Application.Dtos.Order;

namespace SecPerf.Application.Services
{
    public interface IOrderService
    {
        Task<OrderResponseDto?> GetByIdAsync(Guid id);
        Task<IEnumerable<OrderResponseDto>> GetByUserAsync(Guid userId);
        Task<OrderResponseDto> CreateAsync(OrderRequestDto dto);
        Task UpdateStatusAsync(Guid id, SecPerf.Domain.Entities.OrderStatus status);
        Task DeleteAsync(Guid id);
    }
}
