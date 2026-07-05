using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace skipper_api.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    private readonly HealthCheckService _healthCheckService;

    public HealthController(HealthCheckService healthCheckService)
    {
        _healthCheckService = healthCheckService;
    }

    /// <summary>
    /// Liveness check — confirms the API process is running.
    /// This endpoint does not check database connectivity.
    /// Use GET /health/db to verify PostgreSQL reachability.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
    public IActionResult Get() => Ok("Healthy");

    /// <summary>
    /// Database connectivity check — verifies that the API can reach PostgreSQL via ProfessorDbContext.
    /// Returns 200 Healthy when the database is reachable, 503 Unhealthy otherwise.
    /// </summary>
    [HttpGet("db")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable, Type = typeof(string))]
    public async Task<IActionResult> GetDb()
    {
        var report = await _healthCheckService.CheckHealthAsync(
            check => check.Tags.Contains("db"));

        return report.Status == HealthStatus.Healthy
            ? Ok("Healthy")
            : StatusCode(StatusCodes.Status503ServiceUnavailable, "Unhealthy");
    }
}
