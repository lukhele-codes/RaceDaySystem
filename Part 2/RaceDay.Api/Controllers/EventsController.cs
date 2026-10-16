using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using System.Security.Claims;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly AppDbContext _context;

    public EventsController(AppDbContext context)
    {
        _context = context;
    }

    private static EventDto ToDto(Event e) => new()
    {
        Id = e.EventId,
        Name = e.EventName,
        Description = e.Description,
        EventDate = e.EventDate,
        Location = e.Location,
        EventType = e.EventType,
        DistanceKm = e.DistanceKm,
        OrganiserId = e.OrganiserId
    };

    // Admins can manage any event; organisers only their own
    private bool CanManage(Event ev)
    {
        if (User.IsInRole("Admin")) return true;

        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue("sub");

        return int.TryParse(claim, out var userId) && ev.OrganiserId == userId;
    }

    // GET api/events?search=cape&from=2026-11-01
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EventDto>>> GetEvents(
        string? search, DateTime? from)
    {
        var query = _context.Events.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(e => e.EventName.Contains(search) || e.Location.Contains(search));

        if (from.HasValue)
            query = query.Where(e => e.EventDate >= from.Value);

        var events = await query
            .OrderBy(e => e.EventDate)
            .ToListAsync();

        return Ok(events.Select(ToDto));
    }

    // GET api/events/5
    [HttpGet("{id}")]
    public async Task<ActionResult<EventDto>> GetEvent(int id)
    {
        var e = await _context.Events.FindAsync(id);
        if (e == null) return NotFound();

        return Ok(ToDto(e));
    }

    // POST api/events
    [HttpPost]
    [Authorize(Roles = "Admin,Organiser")]
    public async Task<ActionResult<EventDto>> CreateEvent(EventWriteDto dto)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                          ?? User.FindFirstValue("sub");

        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized("User id not found in token.");

        var e = new Event
        {
            EventName = dto.Name,
            Description = dto.Description,
            EventDate = dto.EventDate,
            Location = dto.Location,
            EventType = dto.EventType,
            DistanceKm = dto.DistanceKm,
            OrganiserId = userId
        };

        _context.Events.Add(e);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetEvent), new { id = e.EventId }, ToDto(e));
    }

    // PUT api/events/5 - owning Organiser or Admin
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Organiser")]
    public async Task<IActionResult> UpdateEvent(int id, EventWriteDto dto)
    {
        var e = await _context.Events.FindAsync(id);
        if (e == null) return NotFound();
        if (!CanManage(e)) return Forbid();

        e.EventName = dto.Name;
        e.Description = dto.Description;
        e.EventDate = dto.EventDate;
        e.Location = dto.Location;
        e.EventType = dto.EventType;
        e.DistanceKm = dto.DistanceKm;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // DELETE api/events/5 - owning Organiser or Admin
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Organiser")]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var e = await _context.Events.FindAsync(id);
        if (e == null) return NotFound();
        if (!CanManage(e)) return Forbid();

        _context.Events.Remove(e);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}