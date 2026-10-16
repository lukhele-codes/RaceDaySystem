using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;      // adjust: copy from ResultsController
using RaceDay.Api.DTOs;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;   // adjust: same name as in ResultsController

    public UsersController(AppDbContext db) => _db = db;

    // GET api/users/me
    [HttpGet("me")]
    public async Task<ActionResult<UserProfileDto>> GetMe()
    {
        var user = await _db.Users.FindAsync(CurrentUserId());
        if (user == null) return NotFound();
        return Ok(ToDto(user));
    }

    // PUT api/users/me
    [HttpPut("me")]
    public async Task<ActionResult<UserProfileDto>> UpdateMe(UserUpdateDto dto)
    {
        var user = await _db.Users.FindAsync(CurrentUserId());
        if (user == null) return NotFound();

        var fullName = dto.FullName.Trim();
        var email = dto.Email.Trim();

        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email))
            return BadRequest("Full name and email cannot be blank.");

        // Email must stay unique across users
        var emailTaken = await _db.Users.AnyAsync(u =>
            u.UserId != user.UserId && u.Email.ToLower() == email.ToLower());
        if (emailTaken)
            return BadRequest("That email address is already in use.");

        user.FullName = fullName;
        user.Email = email;
        await _db.SaveChangesAsync();

        return Ok(ToDto(user));
    }

    private UserProfileDto ToDto(Models.User u) => new()
    {
        UserId = u.UserId,
        FullName = u.FullName,
        Email = u.Email,
        Role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty,
        CreatedAt = u.CreatedAt
    };

    private int CurrentUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}