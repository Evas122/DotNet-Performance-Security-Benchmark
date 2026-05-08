using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SecPerf.Application.Dtos.Product;
using SecPerf.Application.Features.Products.Commands.CreateProduct;
using SecPerf.Application.Features.Products.Commands.UpdateProduct;
using SecPerf.Application.Features.Products.Commands.DeleteProduct;
using SecPerf.Application.Features.Products.Queries.GetProductById;
using SecPerf.Application.Features.Products.Queries.GetAllProducts;
using SecPerf.Domain.Common;

namespace SecPerf.ApiMvc.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly ISender _sender;
        public ProductsController(ISender sender) => _sender = sender;

        [HttpPost]
        [ProducesResponseType(typeof(ProductDto), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Create([FromBody] CreateProductRequest req)
            => await _sender.Send(new CreateProductCommand(req)) is Result<ProductDto> r ? (IActionResult)(r.IsSuccess ? Ok(r.Value) : BadRequest(new { error = r.Error?.Message })) : Problem();

        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(ProductDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateProductRequest req)
            => await _sender.Send(new UpdateProductCommand(id, req)) is Result<ProductDto> r ? (IActionResult)(r.IsSuccess ? Ok(r.Value) : r.Error?.Code == "NotFound" ? NotFound(new { error = r.Error.Message }) : BadRequest(new { error = r.Error?.Message })) : Problem();

        [HttpDelete("{id:guid}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete([FromRoute] Guid id)
            => await _sender.Send(new DeleteProductCommand(id)) is Result r ? (IActionResult)(r.IsSuccess ? Ok() : NotFound(new { error = r.Error?.Message })) : Problem();

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ProductDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
            => await _sender.Send(new GetProductByIdQuery(id)) is Result<ProductDto> r ? (IActionResult)(r.IsSuccess ? Ok(r.Value) : NotFound(new { error = r.Error?.Message })) : Problem();

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ProductDto>), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
            => await _sender.Send(new GetAllProductsQuery(page, pageSize)) is Result<IEnumerable<ProductDto>> r ? (IActionResult)(r.IsSuccess ? Ok(r.Value) : BadRequest(new { error = r.Error?.Message })) : Problem();
    }
}
