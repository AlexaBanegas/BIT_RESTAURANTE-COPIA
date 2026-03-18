using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Configuration;
using System.Linq; 
using Microsoft.IdentityModel.Tokens;

namespace BIT_PRUEBA_BE.Data
{
    public class JwtManager
    {
        public static string GenerateToken(string username, string role, string nombreReal, List<string> puntosVenta)
        {
            var secretKey = ConfigurationManager.AppSettings["JWT_SECRET_KEY"];
            var issuer = ConfigurationManager.AppSettings["JWT_ISSUER_TOKEN"];
            var audience = ConfigurationManager.AppSettings["JWT_AUDIENCE_TOKEN"];
            int expireMinutes = Convert.ToInt32(ConfigurationManager.AppSettings["JWT_EXPIRE_MINUTES"]);

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, username),
                new Claim(ClaimTypes.Role, role),
                new Claim("NombreCompleto", nombreReal),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

      
            if (puntosVenta != null)
            {
                foreach (var pv in puntosVenta)
                {
                    claims.Add(new Claim("PuntoVenta", pv.Trim()));
                }
            }

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
               expires: DateTime.UtcNow.AddMinutes(expireMinutes),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}