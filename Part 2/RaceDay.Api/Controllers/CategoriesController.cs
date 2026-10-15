using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;

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

    // GET api/events/5/categories
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategories(int eventId)
    {
        if (!await _context.Events.AnyAsync(e => e.EventId == eventId))
            return NotFound("Event not found.");

        var categories = await _context.Categories
            .Where(c => c.EventId == eventId)
            .Select(c => new CategoryDto
            {
                Id = c.CategoryId,
                EventId = c.EventId,
                Name = c.CategoryName,
                DistanceKm = c.DistanceKm,
                EntryFee = c.EntryFee
            })
            .ToListAsync();

        return Ok(categories);
    }

    // POST api/events/5/categories
    [HttpPost]
    [Authorize(Roles = "Admin,Organiser")]
    public async Task<ActionResult<CategoryDto>> CreateCategory(int eventId, CategoryWriteDto dto)
    {
        if (!await _context.Events.AnyAsync(e => e.EventId == eventId))
            return NotFound("Event not found.");

        var c = new Category
        {
            EventId = eventId,
            CategoryName = dto.Name,
            DistanceKm = dto.DistanceKm,
            EntryFee = dto.EntryFee
        };

        _context.Categories.Add(c);
        await _context.SaveChangesAsync();

        var result = new CategoryDto
        {
            Id = c.CategoryId,
            EventId = c.EventId,
            Name = c.CategoryName,
            DistanceKm = c.DistanceKm,
            EntryFee = c.EntryFee
        };

        return CreatedAtAction(nameof(GetCategories), new { eventId }, result);
    }

    // PUT api/events/5/categories/3
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Organiser")]
    public async Task<IActionResult> UpdateCategory(int eventId, int id, CategoryWriteDto dto)
    {
        var c = await _context.Categories
            .FirstOrDefaultAsync(x => x.EventId == id && x.EventId == eventId);
        if (c == null) return NotFound();

        c.CategoryName = dto.Name;
        c.DistanceKm = dto.DistanceKm;
        c.EntryFee = dto.EntryFee;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // DELETE api/events/5/categories/3
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Organiser")]
    public async Task<IActionResult> DeleteCategory(int eventId, int id)
    {
        var c = await _context.Categories
            .FirstOrDefaultAsync(x => x.EventId == id && x.EventId == eventId);
        if (c == null) return NotFound();

        _context.Categories.Remove(c);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}