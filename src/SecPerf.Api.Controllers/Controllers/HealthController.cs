using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace SecPerf.ApiMvc.Controllers
{
    [ApiController]
    public class HealthController : ControllerBase
    {
        [HttpGet("/health")]
        [ProducesResponseType(200)]
        public IActionResult Health() => Ok(new { timestamp = DateTime.UtcNow });

        [HttpGet("/api/info")]
        [ProducesResponseType(200)]
        public IActionResult Info([FromServices] IHostEnvironment env)
        {
            var entry = Assembly.GetEntryAssembly();
            var version = entry?.GetName()?.Version?.ToString() ?? "unknown";
            return Ok(new { apiType = "controllers", version, environment = env.EnvironmentName });
        }
    }
}
