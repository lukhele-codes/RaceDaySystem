using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Clients;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Api.Token;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TokenServices _tokens;

    public AuthController(AppDbContext db, TokenServices tokens)
    {
        _db = db;
        _tokens = tokens;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(Register dto)
    {

        if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
            return Conflict("That email is already registered.");

        // Only Organiser or Participant can be chosen at registration - never Admin
        var allowed = new[] { "Organiser", "Participant" };
        var requestedRole = allowed.FirstOrDefault(r =>
            r.Equals(dto.Role?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (requestedRole == null)
            return BadRequest("Role must be either Organiser or Participant.");

        var role = await _db.Roles.FirstOrDefaultAsync(r => r.RoleName == requestedRole);
        if (role == null)
            return BadRequest("That role does not exist.");

        var user = new User
        {
            FullName = dto.FirstName,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            RoleId = role.RoleId
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Ok(new AuthResponse
        {
            Token = _tokens.CreateToken(user, role.RoleName),
            Email = user.Email,
            Role = role.RoleName
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(Login dto)
    {
        var user = await _db.Users.Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == dto.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return Unauthorized("Invalid email or password.");

        return Ok(new AuthResponse
        {
            Token = _tokens.CreateToken(user, user.Role.RoleName),
            Email = user.Email,
            Role = user.Role.RoleName
        });
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("admin-only")]
    public IActionResult AdminOnly() => Ok("Welcome, Admin.");

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me() => Ok(new
    {
        Email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value,
        Role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
    });

    public string Role { get; set; } = "Participant";
}