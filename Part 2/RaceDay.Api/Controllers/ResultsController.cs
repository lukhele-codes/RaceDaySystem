using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;      
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api")]
public class ResultsController : ControllerBase
{
    private readonly AppDbContext _db;   
    public ResultsController(AppDbContext db) => _db = db;

    // GET api/categories/2/results - public, ranked (positions first, then by time)
    [HttpGet("categories/{categoryId}/results")]
    public async Task<ActionResult<IEnumerable<ResultDto>>> GetByCategory(int categoryId)
    {
        var list = await _db.Results
            .Where(r => r.Enrolment.CategoryId == categoryId)
            .OrderBy(r => r.Position == null)
            .ThenBy(r => r.Position)
            .ThenBy(r => r.FinishTime)
            .Select(r => new ResultDto
            {
                Id = r.ResultId,
                EnrolmentId = r.EnrolmentId,
                ParticipantId = r.Enrolment.ParticipantId,
                CategoryId = r.Enrolment.CategoryId,
                CategoryName = r.Enrolment.Category.CategoryName,
                FinishTime = r.FinishTime,
                Position = r.Position,
                CapturedAt = r.CapturedAt
            }).ToListAsync();
        return Ok(list);
    }

    // POST api/results - owning Organiser or Admin
    [HttpPost("results")]
    [Authorize(Roles = "Organiser,Admin")]   
    public async Task<ActionResult<ResultDto>> Capture(ResultCreateDto dto)
    {
        var enrolment = await _db.Enrolments
            .Include(e => e.Category)
            .FirstOrDefaultAsync(e => e.EnrolmentId == dto.EnrolmentId);
        if (enrolment == null) return NotFound("Enrolment not found.");

        var ev = await _db.Events.FindAsync(enrolment.Category.EventId);
        if (ev == null || !OwnsEvent(ev)) return Forbid();

        if (enrolment.Status != "Confirmed")
            return Conflict("This enrolment is not confirmed.");

        if (await _db.Results.AnyAsync(r => r.EnrolmentId == enrolment.EnrolmentId))
            return Conflict("A result already exists for this enrolment. Use PUT to change it.");

        var invalid = Validate(dto);
        if (invalid != null) return BadRequest(invalid);

        var result = new Result
        {
            EnrolmentId = enrolment.EnrolmentId,
            FinishTime = dto.FinishTime,
            Position = dto.Position,
            CapturedByUserId = CurrentUserId(),
            CapturedAt = DateTime.UtcNow
        };
        _db.Results.Add(result);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetByCategory),
            new { categoryId = enrolment.CategoryId }, ToDto(result, enrolment));
    }

    // PUT api/results/7 - correct a result
    [HttpPut("results/{id}")]
    [Authorize(Roles = "Organiser,Admin")]
    public async Task<IActionResult> Update(int id, ResultWriteDto dto)
    {
        var result = await _db.Results
            .Include(r => r.Enrolment).ThenInclude(e => e.Category)
            .FirstOrDefaultAsync(r => r.ResultId == id);
        if (result == null) return NotFound();

        var ev = await _db.Events.FindAsync(result.Enrolment.Category.EventId);
        if (ev == null || !OwnsEvent(ev)) return Forbid();

        var invalid = Validate(dto);
        if (invalid != null) return BadRequest(invalid);

        result.FinishTime = dto.FinishTime;
        result.Position = dto.Position;
        result.CapturedByUserId = CurrentUserId();
        result.CapturedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // DELETE api/results/7
    [HttpDelete("results/{id}")]
    [Authorize(Roles = "Organiser,Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _db.Results
            .Include(r => r.Enrolment).ThenInclude(e => e.Category)
            .FirstOrDefaultAsync(r => r.ResultId == id);
        if (result == null) return NotFound();

        var ev = await _db.Events.FindAsync(result.Enrolment.Category.EventId);
        if (ev == null || !OwnsEvent(ev)) return Forbid();

        _db.Results.Remove(result);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // GET api/results/mine - the logged-in participant's own results
    [HttpGet("results/mine")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<ResultDto>>> GetMine()
    {
        var userId = CurrentUserId();
        var list = await _db.Results
            .Where(r => r.Enrolment.ParticipantId == userId)
            .OrderByDescending(r => r.CapturedAt)
            .Select(r => new ResultDto
            {
                Id = r.ResultId,
                EnrolmentId = r.EnrolmentId,
                ParticipantId = r.Enrolment.ParticipantId,
                CategoryId = r.Enrolment.CategoryId,
                CategoryName = r.Enrolment.Category.CategoryName,
                FinishTime = r.FinishTime,
                Position = r.Position,
                CapturedAt = r.CapturedAt
            }).ToListAsync();
        return Ok(list);
    }

    private static string? Validate(ResultWriteDto dto)
    {
        if (dto.FinishTime == null && dto.Position == null)
            return "Provide a finish time, a position, or both.";
        if (dto.FinishTime is { } t && t <= TimeSpan.Zero)
            return "Finish time must be greater than zero.";
        return null;
    }

    private static ResultDto ToDto(Result r, Enrolment e) => new()
    {
        Id = r.ResultId,
        EnrolmentId = r.EnrolmentId,
        ParticipantId = e.ParticipantId,
        CategoryId = e.CategoryId,
        CategoryName = e.Category.CategoryName,
        FinishTime = r.FinishTime,
        Position = r.Position,
        CapturedAt = r.CapturedAt
    };

    private int CurrentUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // Admins can touch anything; Organisers only their own events
    private bool OwnsEvent(Event ev)
    {
        if (User.IsInRole("Admin")) return true;
        return ev.OrganiserId == CurrentUserId();
    }
}