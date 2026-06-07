using GridTelemetry.Core.Data;
using GridTelemetry.Core.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GridTelemetry.Api.Controller;

[ApiController]
[Route("api/metrics")]
public class MetricController(ApplicationDbContext dbContext) : ControllerBase
{
    [HttpGet("v1/snapshot")]
    public async Task<ActionResult<IEnumerable<SubstationMetric>>> GetLatestSnapshot()
    {
        var latestTimestampsQuery = dbContext.SubstationMetrics
            .AsNoTracking()
            .GroupBy(m => m.SubstationCode)
            .Select(g => new
            {
                SubstationCode = g.Key,
                MaxTimestamp = g.Max(m => m.Timestamp)
            });

        var snapshot = await dbContext.SubstationMetrics
            .AsNoTracking()
            .Join(
                latestTimestampsQuery,
                outer => new { outer.SubstationCode, outer.Timestamp },
                inner => new { inner.SubstationCode, Timestamp = inner.MaxTimestamp },
                (outer, inner) => outer
            )
            .ToListAsync();

        return Ok(snapshot);
    }
}