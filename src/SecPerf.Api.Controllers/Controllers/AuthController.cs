using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecPerf.Application.Dtos.Auth;
using SecPerf.Application.Features.Auth.Commands.Login;
using SecPerf.Application.Features.Auth.Commands.Register;
using SecPerf.Application.Features.Auth.Commands.RefreshToken;
using SecPerf.Application.Features.Auth.Commands.RevokeToken;
using SecPerf.Domain.Common;

namespace SecPerf.ApiMvc.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ISender _sender;
        public AuthController(ISender sender) => _sender = sender;

        [HttpPost("register")]
        [ProducesResponseType(typeof(AuthResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest req)
            => await _sender.Send(new RegisterCommand(req)) is Result<AuthResponse> r ? (IActionResult)(r.IsSuccess ? Ok(r.Value) : r.Error?.Code == "Conflict" ? Conflict(new { error = r.Error.Message }) : BadRequest(new { error = r.Error?.Message })) : Problem();

        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
            => await _sender.Send(new LoginCommand(req)) is Result<AuthResponse> r ? (IActionResult)(r.IsSuccess ? Ok(r.Value) : r.Error?.Code == "InvalidCredentials" ? Unauthorized() : BadRequest(new { error = r.Error?.Message })) : Problem();

        [HttpPost("refresh")]
        [ProducesResponseType(typeof(AuthResponse), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Refresh([FromBody] string refreshToken)
            => await _sender.Send(new RefreshTokenCommand(refreshToken)) is Result<AuthResponse> r ? (IActionResult)(r.IsSuccess ? Ok(r.Value) : BadRequest(new { error = r.Error?.Message })) : Problem();

        [Authorize]
        [HttpPost("revoke")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Revoke([FromBody] string refreshToken)
            => await _sender.Send(new RevokeTokenCommand(refreshToken)) is Result r ? (IActionResult)(r.IsSuccess ? Ok() : BadRequest(new { error = r.Error?.Message })) : Problem();
    }
}
