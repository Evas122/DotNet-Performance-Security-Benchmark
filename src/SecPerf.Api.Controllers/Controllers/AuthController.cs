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
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestBody req)
            => await _sender.Send(new RefreshTokenCommand(req.RefreshToken)) is Result<AuthResponse> r ? (IActionResult)(r.IsSuccess ? Ok(r.Value) : BadRequest(new { error = r.Error?.Message })) : Problem();

        [Authorize]
        [HttpPost("revoke")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> Revoke([FromBody] RevokeTokenRequestBody req)
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                            ?? User.FindFirst("sub")?.Value;
            if (!Guid.TryParse(userIdStr, out var callerId)) return Unauthorized();

            var r = await _sender.Send(new RevokeTokenCommand(req.RefreshToken, callerId));
            if (r == null) return Problem();
            if (r.IsSuccess) return Ok();
            if (r.Error?.Code == "Forbidden") return StatusCode(StatusCodes.Status403Forbidden, new { error = r.Error.Message });
            return BadRequest(new { error = r.Error?.Message });
        }

        public record RefreshTokenRequestBody(string RefreshToken);
        public record RevokeTokenRequestBody(string RefreshToken);
    }
}
