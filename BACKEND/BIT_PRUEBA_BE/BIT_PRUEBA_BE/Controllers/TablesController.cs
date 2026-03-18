using BIT_PRUEBA_BE.Data;
using BIT_PRUEBA_BE.Enums;
using System;
using System.Web.Http;
using System.Security.Claims;
using System.Linq;
using Serilog;

namespace BIT_PRUEBA_BE.Controllers
{
    [Authorize]
    [RoutePrefix("api/tables")]
    public class TablesController : ApiController
    {
        private TableData _tableData = new TableData();

        private string GetPuntoVentaSeleccionado()
        {
            var identity = User.Identity as ClaimsIdentity;
            if (identity == null) return null;

            var pvsPermitidos = identity.FindAll("PuntoVenta").Select(c => c.Value).ToList();
            if (pvsPermitidos.Count == 0) return null;

            var headers = Request.Headers;
            if (headers.Contains("X-Punto-Venta"))
            {
                string pvHeader = headers.GetValues("X-Punto-Venta").First();
                if (pvsPermitidos.Contains(pvHeader)) return pvHeader;
            }

            return pvsPermitidos.FirstOrDefault();
        }

        [HttpGet]
        [Route("")]
        public IHttpActionResult GetTables()
        {
            try
            {
                string connectionString = UsuarioData.GetConnectionString();
                string pv = GetPuntoVentaSeleccionado();

                if (string.IsNullOrEmpty(pv)) return Unauthorized();

                var mesas = _tableData.ListarMesas(pv, connectionString);

                Log.Information(AuthMessages.GetMessage(AuthResult.TableListSuccess).Replace("{0}", mesas.Count.ToString()));
                return Ok(mesas);
            }
            catch (Exception ex)
            {
                Log.Error(ex, AuthMessages.GetMessage(AuthResult.ErrorCargarMesas));
                return BadRequest(AuthMessages.GetMessage(AuthResult.ErrorCargarMesas));
            }
        }

        [HttpPatch]
        [Route("{id}/estado")]
        public IHttpActionResult UpdateEstado(int id, [FromBody] TableStatusUpdateDTO dto)
        {
            try
            {
                string connectionString = UsuarioData.GetConnectionString();
                string pv = GetPuntoVentaSeleccionado();

                if (string.IsNullOrEmpty(pv) || dto == null || id <= 0)
                {
                    return BadRequest(AuthMessages.GetMessage(AuthResult.IncompleteData));
                }

                if (_tableData.ActualizarEstado(id, dto.NuevoEstado, pv, connectionString))
                {
                    string nombreEstado = (dto.NuevoEstado == 1)
                        ? AuthMessages.GetMessage(AuthResult.MesaDisponible)
                        : AuthMessages.GetMessage(AuthResult.MesaOcupada);

                    string mensajeFinal = AuthMessages.GetMessage(AuthResult.UpdateSuccess)
                        .Replace("{Id}", id.ToString())
                        .Replace("{EstadoNombre}", nombreEstado);

                    return Ok(new { message = mensajeFinal });
                }

                return NotFound();
            }
            catch (Exception ex)
            {
                Log.Error(ex, AuthMessages.GetMessage(AuthResult.ErrorUpdateMesa) + " ID: {Id}", id);
                return BadRequest(AuthMessages.GetMessage(AuthResult.ErrorUpdateMesa));
            }
        }
    }
}