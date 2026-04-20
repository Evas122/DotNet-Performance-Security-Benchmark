using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SecPerf.Application.Dtos.Order;
using SecPerf.Domain.Entities;
using SecPerf.Domain.Repositories;

namespace SecPerf.Application.Services.Impl
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IProductRepository _productRepository;
        private readonly IUserRepository _userRepository;

        public OrderService(IOrderRepository orderRepository, IProductRepository productRepository, IUserRepository userRepository)
        {
            _orderRepository = orderRepository;
            _productRepository = productRepository;
            _userRepository = userRepository;
        }

        public async Task<OrderResponseDto> CreateAsync(OrderRequestDto dto)
        {
            var user = await _userRepository.GetByIdAsync(dto.UserId);
            if (user == null) throw new InvalidOperationException("User not found");

            var order = new Order
            {
                Id = Guid.NewGuid(),
                UserId = dto.UserId,
                CreatedAt = DateTime.UtcNow,
                Status = OrderStatus.Pending,
                TotalPrice = 0m
            };

            decimal total = 0m;
            var items = new List<OrderItem>();
            foreach (var it in dto.Items)
            {
                var p = await _productRepository.GetByIdAsync(it.ProductId);
                if (p == null) throw new InvalidOperationException($"Product {it.ProductId} not found");
                var unitPrice = p.Price;
                var oi = new OrderItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    ProductId = p.Id,
                    Quantity = it.Quantity,
                    UnitPrice = unitPrice
                };
                items.Add(oi);
                total += unitPrice * it.Quantity;
            }

            order.TotalPrice = Math.Round(total, 2);

            await _orderRepository.AddAsync(order);
            await _orderRepository.SaveChangesAsync();

            // add items
            foreach (var oi in items)
            {
                // attach via context - through repository pattern we don't have direct OrderItem repo; use Order navigation
                // simpler approach: use OrderRepository's context by creating OrderItems via repository update
            }

            // naive: directly set OrderItems and update
            order.OrderItems = items;
            await _orderRepository.UpdateAsync(order);
            await _orderRepository.SaveChangesAsync();

            return Map(order);
        }

        public Task DeleteAsync(Guid id)
        {
            var o = _orderRepository.Query().FirstOrDefault(x => x.Id == id);
            if (o == null) return Task.CompletedTask;
            _orderRepository.DeleteAsync(o);
            return _orderRepository.SaveChangesAsync();
        }

        public Task<OrderResponseDto?> GetByIdAsync(Guid id)
        {
            var o = _orderRepository.Query().Where(x => x.Id == id).FirstOrDefault();
            if (o == null) return Task.FromResult<OrderResponseDto?>(null);
            return Task.FromResult(Map(o));
        }

        public Task<IEnumerable<OrderResponseDto>> GetByUserAsync(Guid userId)
        {
            var list = _orderRepository.QueryByUser(userId).Select(Map).ToList();
            return Task.FromResult((IEnumerable<OrderResponseDto>)list);
        }

        public async Task UpdateStatusAsync(Guid id, OrderStatus status)
        {
            var o = await _orderRepository.GetByIdAsync(id);
            if (o == null) return;
            o.Status = status;
            await _orderRepository.UpdateAsync(o);
            await _orderRepository.SaveChangesAsync();
        }

        private static OrderResponseDto Map(Order o)
        {
            return new OrderResponseDto
            {
                Id = o.Id,
                UserId = o.UserId,
                CreatedAt = o.CreatedAt,
                Status = o.Status,
                TotalPrice = o.TotalPrice,
                Items = o.OrderItems?.Select(i => new OrderItemResponseDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList() ?? new List<OrderItemResponseDto>()
            };
        }
    }
}
