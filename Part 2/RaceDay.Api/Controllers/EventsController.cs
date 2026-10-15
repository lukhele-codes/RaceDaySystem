using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
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
            .Select(e => new EventDto
            {
                Id = e.EventId,
                Name = e.EventName,
                Description = e.Description,
                EventDate = e.EventDate,
                Location = e.Location
            })
            .ToListAsync();

        return Ok(events);
    }

    // GET api/events/5
    [HttpGet("{id}")]
    public async Task<ActionResult<EventDto>> GetEvent(int id)
    {
        var e = await _context.Events.FindAsync(id);
        if (e == null) return NotFound();

        return Ok(new EventDto
        {
            Id = e.EventId,
            Name = e.EventName,
            Description = e.Description,
            EventDate = e.EventDate,
            Location = e.Location
        });
    }

    // POST api/events
    [HttpPost]
    [Authorize(Roles = "Admin,Organiser")]
    public async Task<ActionResult<EventDto>> CreateEvent(EventWriteDto dto)
    {
        var e = new Event
        {
            EventName = dto.Name,
            Description = dto.Description,
            EventDate = dto.EventDate,
            Location = dto.Location
        };

        _context.Events.Add(e);
        await _context.SaveChangesAsync();

        var result = new EventDto
        {
            Id = e.EventId,
            Name = e.EventName,
            Description = e.Description,
            EventDate = e.EventDate,
            Location = e.Location
        };

        return CreatedAtAction(nameof(GetEvent), new { id = e.EventId }, result);
    }

    // PUT api/events/5
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Organiser")]
    public async Task<IActionResult> UpdateEvent(int id, EventWriteDto dto)
    {
        var e = await _context.Events.FindAsync(id);
        if (e == null) return NotFound();

        e.EventName = dto.Name;
        e.Description = dto.Description;
        e.EventDate = dto.EventDate;
        e.Location = dto.Location;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // DELETE api/events/5
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var e = await _context.Events.FindAsync(id);
        if (e == null) return NotFound();

        _context.Events.Remove(e);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}