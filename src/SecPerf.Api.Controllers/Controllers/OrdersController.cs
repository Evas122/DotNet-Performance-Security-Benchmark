using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecPerf.Application.Dtos.Order;
using SecPerf.Application.Features.Orders.Queries.GetAllOrders;
using SecPerf.Application.Features.Orders.Queries.GetOrderById;
using SecPerf.Domain.Common;

namespace SecPerf.ApiMvc.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly ISender _sender;
        public OrdersController(ISender sender) => _sender = sender;

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<OrderDto>), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
            => await _sender.Send(new GetAllOrdersQuery(page, pageSize)) is Result<IEnumerable<OrderDto>> r
                ? (IActionResult)(r.IsSuccess ? Ok(r.Value) : BadRequest(new { error = r.Error?.Message }))
                : Problem();

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(OrderDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
            => await _sender.Send(new GetOrderByIdQuery(id)) is Result<OrderDto> r
                ? (IActionResult)(r.IsSuccess ? Ok(r.Value) : NotFound(new { error = r.Error?.Message }))
                : Problem();
    }
}
