using Microsoft.Owin;
using Microsoft.Owin.Security.Jwt;
using Microsoft.IdentityModel.Tokens;
using Owin;
using System.Configuration;
using System.Text;
using System.Web.Http;
using System;

[assembly: OwinStartup(typeof(BIT_PRUEBA_BE.Startup))]

namespace BIT_PRUEBA_BE
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var secretKey = ConfigurationManager.AppSettings["JWT_SECRET_KEY"];
            var issuer = ConfigurationManager.AppSettings["JWT_ISSUER_TOKEN"];
            var audience = ConfigurationManager.AppSettings["JWT_AUDIENCE_TOKEN"];
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

            app.UseJwtBearerAuthentication(new JwtBearerAuthenticationOptions
            {
                AuthenticationMode = Microsoft.Owin.Security.AuthenticationMode.Active,
                AllowedAudiences = new[] { audience },
                TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = securityKey,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }
            });

            HttpConfiguration config = GlobalConfiguration.Configuration;
            config.MapHttpAttributeRoutes();
            WebApiConfig.Register(config);
            config.EnsureInitialized();
            app.UseWebApi(config);
        }
    }
}