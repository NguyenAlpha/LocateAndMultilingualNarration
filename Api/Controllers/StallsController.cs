using Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController]
[Route("api/stalls")]
[AllowAnonymous]
public class StallsController : ControllerBase
{
    private readonly AppDbContext _context;

    public StallsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetStalls(CancellationToken cancellationToken)
    {
        var stalls = await _context.Stalls
            .AsNoTracking()
            .Where(s => s.IsActive)
            .Select(s => new StallMapDto
            {
                Id = s.Id,
                Name = s.Name,
                Latitude = s.StallLocations
                    .Where(l => l.IsActive)
                    .OrderByDescending(l => l.UpdatedAt)
                    .Select(l => (double?)l.Latitude)
                    .FirstOrDefault() ?? 0,
                Longitude = s.StallLocations
                    .Where(l => l.IsActive)
                    .OrderByDescending(l => l.UpdatedAt)
                    .Select(l => (double?)l.Longitude)
                    .FirstOrDefault() ?? 0,
                AudioUrl = s.StallNarrationContents
                    .SelectMany(n => n.NarrationAudios)
                    .Where(a => a.IsTts && a.AudioUrl != null)
                    .OrderByDescending(a => a.UpdatedAt)
                    .Select(a => a.AudioUrl!)
                    .FirstOrDefault() ?? string.Empty
            })
            .Where(x => x.Latitude != 0 && x.Longitude != 0)
            .ToListAsync(cancellationToken);

        return Ok(stalls);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetStallById(Guid id, CancellationToken cancellationToken)
    {
        var stall = await _context.Stalls
            .AsNoTracking()
            .Where(s => s.IsActive && s.Id == id)
            .Select(s => new StallMapDto
            {
                Id = s.Id,
                Name = s.Name,
                Latitude = s.StallLocations
                    .Where(l => l.IsActive)
                    .OrderByDescending(l => l.UpdatedAt)
                    .Select(l => (double?)l.Latitude)
                    .FirstOrDefault() ?? 0,
                Longitude = s.StallLocations
                    .Where(l => l.IsActive)
                    .OrderByDescending(l => l.UpdatedAt)
                    .Select(l => (double?)l.Longitude)
                    .FirstOrDefault() ?? 0,
                AudioUrl = s.StallNarrationContents
                    .SelectMany(n => n.NarrationAudios)
                    .Where(a => a.IsTts && a.AudioUrl != null)
                    .OrderByDescending(a => a.UpdatedAt)
                    .Select(a => a.AudioUrl!)
                    .FirstOrDefault() ?? string.Empty
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (stall is null)
        {
            return NotFound();
        }

        return Ok(stall);
    }

    public class StallMapDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string AudioUrl { get; set; } = string.Empty;
    }
}
