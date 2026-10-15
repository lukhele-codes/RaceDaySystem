using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using System.Security.Claims;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/events/{eventId}/categories")]
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _context;

    public CategoriesController(AppDbContext context)
    {
        _context = context;
    }

    // Admins can manage any event; organisers only their own
    private bool CanManage(Event ev)
    {
        if (User.IsInRole("Admin")) return true;

        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue("sub");

        return int.TryParse(claim, out var userId) && ev.OrganiserId == userId;
    }

    private static CategoryDto ToDto(Category c) => new()
    {
        Id = c.CategoryId,
        EventId = c.EventId,
        Name = c.CategoryName,
        DistanceKm = c.DistanceKm,
        MaxParticipants = c.MaxParticipants,
        EntryFee = c.EntryFee
    };

    // GET api/events/5/categories
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategories(int eventId)
    {
        if (!await _context.Events.AnyAsync(e => e.EventId == eventId))
            return NotFound("Event not found.");

        var categories = await _context.Categories
            .Where(c => c.EventId == eventId)
            .OrderBy(c => c.DistanceKm)
            .ToListAsync();

        return Ok(categories.Select(ToDto));
    }

    // GET api/events/5/categories/3
    [HttpGet("{id}")]
    public async Task<ActionResult<CategoryDto>> GetCategory(int eventId, int id)
    {
        var c = await _context.Categories
            .FirstOrDefaultAsync(x => x.CategoryId == id && x.EventId == eventId);

        if (c == null) return NotFound();
        return Ok(ToDto(c));
    }

    // POST api/events/5/categories
    [HttpPost]
    [Authorize(Roles = "Admin,Organiser")]
    public async Task<ActionResult<CategoryDto>> CreateCategory(int eventId, CategoryWriteDto dto)
    {
        var ev = await _context.Events.FindAsync(eventId);
        if (ev == null) return NotFound("Event not found.");
        if (!CanManage(ev)) return Forbid();

        var c = new Category
        {
            EventId = eventId,
            CategoryName = dto.Name,
            DistanceKm = dto.DistanceKm,
            MaxParticipants = dto.MaxParticipants,
            EntryFee = dto.EntryFee
        };

        _context.Categories.Add(c);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCategory),
            new { eventId, id = c.CategoryId }, ToDto(c));
    }

    // PUT api/events/5/categories/3
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Organiser")]
    public async Task<IActionResult> UpdateCategory(int eventId, int id, CategoryWriteDto dto)
    {
        var ev = await _context.Events.FindAsync(eventId);
        if (ev == null) return NotFound("Event not found.");
        if (!CanManage(ev)) return Forbid();

        var c = await _context.Categories
            .FirstOrDefaultAsync(x => x.CategoryId == id && x.EventId == eventId);
        if (c == null) return NotFound();

        c.CategoryName = dto.Name;
        c.DistanceKm = dto.DistanceKm;
        c.MaxParticipants = dto.MaxParticipants;
        c.EntryFee = dto.EntryFee;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // DELETE api/events/5/categories/3
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Organiser")]
    public async Task<IActionResult> DeleteCategory(int eventId, int id)
    {
        var ev = await _context.Events.FindAsync(eventId);
        if (ev == null) return NotFound("Event not found.");
        if (!CanManage(ev)) return Forbid();

        var c = await _context.Categories
            .FirstOrDefaultAsync(x => x.CategoryId == id && x.EventId == eventId);
        if (c == null) return NotFound();

        _context.Categories.Remove(c);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}