using BIT_PRUEBA_BE.Data;
using BIT_PRUEBA_BE.Enums;
using Serilog;
using System.Net;
using System.Web.Http;
using System.Web.Http.Cors;
using Newtonsoft.Json.Linq;
using System;
using System.Web;

namespace BIT_PRUEBA_BE.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    [RoutePrefix("api/auth")]
    public class AuthController : ApiController
    {
        [HttpPost]
        [Route("login")]
        [AllowAnonymous]
        public IHttpActionResult Login([FromBody] JObject request)
        {
            try
            {
                string username = request?["username"]?.ToString();
                string password = request?["password"]?.ToString();
                string companyId = request?["companyId"]?.ToString();
                string puntoVentaId = request?["puntoVentaId"]?.ToString();

                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                    return Ok(new { success = false, message = AuthMessages.GetMessage(AuthResult.IncompleteData) });

                UsuarioData db = new UsuarioData();
                var acceso = db.ValidarAccesoJerarquico(username, password, companyId, puntoVentaId);

                if (acceso == null)
                    return Ok(new { success = false, message = AuthMessages.GetMessage(AuthResult.InvalidCredentials) });

                if (acceso.NextStep != AuthMessages.GetMessage(AuthResult.AllowAccess))
                    return Ok(new { success = true, nextStep = acceso.NextStep, options = acceso.Options });

                var usuarioInfo = acceso.User;

                if (usuarioInfo.Estado != "1" && usuarioInfo.Estado != "A")
                    return Ok(new { success = false, message = AuthMessages.GetMessage(AuthResult.UserInactive) });

                string tokenGenerado = JwtManager.GenerateToken(username, usuarioInfo.Rol, usuarioInfo.NombreCompleto, usuarioInfo.PuntosVenta);
                string refreshToken = Guid.NewGuid().ToString();
                string clientIp = HttpContext.Current?.Request.UserHostAddress;

                new RefreshTokenData().RegistrarNuevoToken(refreshToken, usuarioInfo.IdentityGuid, clientIp, null);

                Log.Information(AuthMessages.GetMessage(AuthResult.LogAccessGranted)
                    .Replace("{UserName}", username).Replace("{Nombre}", usuarioInfo.NombreCompleto).Replace("{Rol}", usuarioInfo.Rol));

                return Ok(new
                {
                    success = true,
                    nextStep = AuthMessages.GetMessage(AuthResult.AllowAccess),
                    token = tokenGenerado,
                    refresh_token = refreshToken,
                    user = new
                    {
                        usuario = username,
                        nombre = usuarioInfo.NombreCompleto,
                        rol = usuarioInfo.Rol,
                        id = usuarioInfo.EmpleadoID,
                        puntosVenta = usuarioInfo.PuntosVenta,
                        compania = usuarioInfo.CompaniaID
                    }
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, AuthMessages.GetMessage(AuthResult.DatabaseErrorLog).Replace("{Username}", "LoginSystem"));
                return InternalServerError();
            }
        }

        [HttpPost]
        [Route("refresh")]
        [AllowAnonymous]
        public IHttpActionResult Refresh([FromBody] JObject request)
        {
            try
            {
                string refreshTokenRecibido = request?["refresh_token"]?.ToString();
                string companyId = request?["companyId"]?.ToString();

                if (string.IsNullOrEmpty(refreshTokenRecibido)) return BadRequest();

                string clientIp = HttpContext.Current?.Request.UserHostAddress;
                RefreshTokenData rtDb = new RefreshTokenData();
                string userIdGuid;
                int oldTokenId;

                if (!rtDb.ValidarYRotar(refreshTokenRecibido, clientIp, out userIdGuid, out oldTokenId))
                    return Content(HttpStatusCode.Unauthorized, new { message = AuthMessages.GetMessage(AuthResult.TokenExpired) });

                string usernameActual = null;
                using (var conn = new System.Data.SqlClient.SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings["BIT_Identity_Conn"].ConnectionString))
                {
                    var cmd = new System.Data.SqlClient.SqlCommand("SELECT UserName FROM AspNetUsers WHERE Id = @id", conn);
                    cmd.Parameters.AddWithValue("@id", userIdGuid);
                    conn.Open();
                    usernameActual = cmd.ExecuteScalar()?.ToString();
                }

                if (string.IsNullOrEmpty(usernameActual)) return Unauthorized();

                UsuarioData userDb = new UsuarioData();
               
                var user = userDb.ObtenerDatosPorNombre(usernameActual, userIdGuid, companyId);

                if (user == null) return Unauthorized();

                string newAccessToken = JwtManager.GenerateToken(usernameActual, user.Rol, user.NombreCompleto, user.PuntosVenta);
                string newRefreshToken = Guid.NewGuid().ToString();

                rtDb.RegistrarNuevoToken(newRefreshToken, userIdGuid, clientIp, oldTokenId);

                return Ok(new { token = newAccessToken, refresh_token = newRefreshToken });
            }
            catch (Exception ex)
            {
                Log.Error(ex, AuthMessages.GetMessage(AuthResult.DatabaseErrorLog).Replace("{Username}", "RefreshSystem"));
                return InternalServerError();
            }
        }
    }
}