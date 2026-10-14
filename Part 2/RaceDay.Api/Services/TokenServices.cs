using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using RaceDay.Api.Models;

namespace RaceDay.Api.Token
{
    public class TokenServices
    {
    private readonly IConfiguration _config;
    public TokenServices(IConfiguration config) => _config = config;

    public string CreateToken(User user, string roleName)
    {
    var claims = new List<Claim>
    {
    new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
    new(ClaimTypes.Email, user.Email),
    new(ClaimTypes.Role, roleName)
    };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
             issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: creds);

    return new JwtSecurityTokenHandler().WriteToken(token);




    }

    }
}
