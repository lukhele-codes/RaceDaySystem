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

        var role = await _db.Roles.FirstAsync(r => r.RoleName == "Participant");

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
}