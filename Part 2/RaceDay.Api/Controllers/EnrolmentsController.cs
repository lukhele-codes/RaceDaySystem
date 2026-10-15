using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;      
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EnrolmentsController : ControllerBase
{
    private readonly AppDbContext _db;   

    public EnrolmentsController(AppDbContext db) => _db = db;

    // GET api/enrolments/mine - the logged-in user's enrolments
    [HttpGet("mine")]
    public async Task<ActionResult<IEnumerable<EnrolmentDto>>> GetMine()
    {
        var userId = CurrentUserId();
        var list = await _db.Enrolments
            .Where(e => e.ParticipantId == userId)
            .Include(e => e.Category)
            .OrderByDescending(e => e.EnrolmentDate)
            .Select(e => new EnrolmentDto
            {
                Id = e.EnrolmentId,
                CategoryId = e.CategoryId,
                CategoryName = e.Category.CategoryName,
                EventId = e.Category.EventId,
                EnrolmentDate = e.EnrolmentDate,
                Status = e.Status
            }).ToListAsync();
        return Ok(list);
    }

    // POST api/enrolments - body: { "categoryId": 1 }
    [HttpPost]
    public async Task<ActionResult<EnrolmentDto>> Enrol(EnrolmentWriteDto dto)
    {
        var userId = CurrentUserId();

        var category = await _db.Categories.FindAsync(dto.CategoryId);
        if (category == null) return NotFound("Category not found.");

        // Capacity check
        var taken = await _db.Enrolments.CountAsync(e =>
            e.CategoryId == category.CategoryId && e.Status == "Confirmed");
        if (taken >= category.MaxParticipants)
            return Conflict("This category is full.");

        // Duplicate check (a cancelled enrolment can be re-activated)
        var existing = await _db.Enrolments.FirstOrDefaultAsync(e =>
            e.ParticipantId == userId && e.CategoryId == category.CategoryId);

        if (existing != null && existing.Status != "Cancelled")
            return Conflict("You are already enrolled in this category.");

        Enrolment enrolment;
        if (existing != null)
        {
            existing.Status = "Confirmed";
            existing.EnrolmentDate = DateTime.UtcNow;
            enrolment = existing;
        }
        else
        {
            enrolment = new Enrolment
            {
                ParticipantId = userId,
                CategoryId = category.CategoryId,
                EnrolmentDate = DateTime.UtcNow,
                Status = "Confirmed"
            };
            _db.Enrolments.Add(enrolment);
        }
        await _db.SaveChangesAsync();

        var result = new EnrolmentDto
        {
            Id = enrolment.EnrolmentId,
            CategoryId = category.CategoryId,
            CategoryName = category.CategoryName,
            EventId = category.EventId,
            EnrolmentDate = enrolment.EnrolmentDate,
            Status = enrolment.Status
        };
        return CreatedAtAction(nameof(GetMine), result);
    }

    // DELETE api/enrolments/5 - cancel your own enrolment
    [HttpDelete("{id}")]
    public async Task<IActionResult> Cancel(int id)
    {
        var enrolment = await _db.Enrolments
            .Include(e => e.Result)
            .FirstOrDefaultAsync(e => e.EnrolmentId == id);
        if (enrolment == null) return NotFound();
        if (enrolment.ParticipantId != CurrentUserId()) return Forbid();
        if (enrolment.Result != null)
            return Conflict("A result has already been captured for this enrolment.");

        enrolment.Status = "Cancelled";
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private int CurrentUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}