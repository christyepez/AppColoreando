using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AppColoreando.Application.Abstractions;
using AppColoreando.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace AppColoreando.Infrastructure.Security;

public sealed class PasswordService(IPasswordHasher<AppUser> hasher) : IPasswordService
{
    public string Hash(AppUser user,string password)=>hasher.HashPassword(user,password);
    public bool Verify(AppUser user,string hash,string password)=>hasher.VerifyHashedPassword(user,hash,password)!=PasswordVerificationResult.Failed;
}

public sealed class TokenService(IConfiguration configuration) : ITokenService
{
    public string Create(AppUser user)
    {
        var key=configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is required.");
        if(key.Length<32) throw new InvalidOperationException("Jwt:Key must be at least 32 characters.");
        var issuer=configuration["Jwt:Issuer"] ?? "AppColoreando";
        var audience=configuration["Jwt:Audience"] ?? "AppColoreando.MobileAdmin";
        var claims=new[]{new Claim(JwtRegisteredClaimNames.Sub,user.Id.ToString()),new Claim(ClaimTypes.NameIdentifier,user.Id.ToString()),new Claim(ClaimTypes.Email,user.Email),new Claim(ClaimTypes.Role,user.Role)};
        var credentials=new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),SecurityAlgorithms.HmacSha256);
        var token=new JwtSecurityToken(issuer,audience,claims,expires:DateTime.UtcNow.AddHours(8),signingCredentials:credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
