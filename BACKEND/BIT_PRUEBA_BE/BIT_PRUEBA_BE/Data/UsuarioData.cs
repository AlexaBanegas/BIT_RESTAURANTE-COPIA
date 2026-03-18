using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using Serilog;
using BIT_PRUEBA_BE.Enums;
using Microsoft.AspNet.Identity;
using System.Linq;
using System.Web;


namespace BIT_PRUEBA_BE.Data
{
    public class UsuarioLoginDTO
    {
        public string EmpleadoID { get; set; }
        public string NombreCompleto { get; set; }
        public string Rol { get; set; }
        public string Estado { get; set; }
        public string IdentityGuid { get; set; }
        public string CompaniaID { get; set; }
        public List<string> PuntosVenta { get; set; } = new List<string>();
    }

    public class LoginResultDTO
    {
        public string NextStep { get; set; }
        public object Options { get; set; }
        public UsuarioLoginDTO User { get; set; }
    }

    public class UsuarioData
    {
        private string GetIdentityConn() => ConfigurationManager.ConnectionStrings["BIT_Identity_Conn"].ConnectionString;

        public static string GetConnectionString(string companyId = null)
        {
            var context = HttpContext.Current;
            string id = (companyId ?? context?.Request.Headers["X-Company-Id"] ?? "").Trim().ToUpper();
            string connName = (id == "FI") ? "BIT_PRUEBA_Conn" : "BIT_FCH_Conn";

            var connectionSetting = ConfigurationManager.ConnectionStrings[connName];
            var builder = new SqlConnectionStringBuilder(connectionSetting.ConnectionString) { Pooling = false };
            return builder.ToString();
        }

        public LoginResultDTO ValidarAccesoJerarquico(string username, string password, string compId, string pvId)
        {
            var userDto = ValidarUsuarioYObtenerDatos(username, password, compId);
            if (userDto == null) return null;

            using (var conn = new SqlConnection(GetIdentityConn()))
            {
                var companias = new List<dynamic>();
                string queryComp = "SELECT DISTINCT c.CompanyId, c.Name FROM Companies c INNER JOIN UserCompanies uc ON c.CompanyId = uc.CompanyId WHERE uc.UserId = @uid AND c.IsActive = 1";
                var cmd = new SqlCommand(queryComp, conn);
                cmd.Parameters.AddWithValue("@uid", userDto.IdentityGuid);
                conn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read()) companias.Add(new { Id = dr["CompanyId"].ToString().Trim(), Name = dr["Name"].ToString().Trim() });
                }

                if (string.IsNullOrEmpty(compId) && companias.Count > 1)
                    return new LoginResultDTO { NextStep = AuthMessages.GetMessage(AuthResult.SelectCompany), Options = companias };

                string finalCompId = (compId ?? companias.FirstOrDefault()?.Id)?.Trim().ToUpper();
                userDto.CompaniaID = finalCompId;

                var pvs = new List<dynamic>();
                using (var connOperativa = new SqlConnection(GetConnectionString(finalCompId)))
                {
                    string queryPv = @"SELECT RT.CODIGO_RESTAURANTE AS Id, PV.DESCRIPCION_PUNTO_VENTA AS Name 
                                      FROM PUNTO_DE_VENTA PV 
                                      INNER JOIN PUNTO_DE_VENTA_USUARIO PVU ON PV.CODIGO_BIC = PVU.CODIGO_BIC 
                                      INNER JOIN RESTAURANTES_TABLA RT ON PV.CODIGO_BIC = RT.PUNTO_DE_VENTA_CODIGO_BIC 
                                      WHERE PVU.OS4sA = @user AND PV.ESTATUS_PUNTO_DE_VENTA = '1'
                                      GROUP BY RT.CODIGO_RESTAURANTE, PV.DESCRIPCION_PUNTO_VENTA";

                    var cmdPv = new SqlCommand(queryPv, connOperativa);
                    cmdPv.Parameters.AddWithValue("@user", username);
                    connOperativa.Open();
                    using (var drPv = cmdPv.ExecuteReader())
                    {
                        while (drPv.Read()) pvs.Add(new { Id = drPv["Id"].ToString().Trim(), Name = drPv["Name"].ToString().Trim() });
                    }
                }

                if (pvs.Count == 0) return null;

                if (string.IsNullOrEmpty(pvId) && pvs.Count > 1)
                    return new LoginResultDTO { NextStep = AuthMessages.GetMessage(AuthResult.SelectRestaurant), Options = pvs };

                userDto.PuntosVenta = new List<string> { pvId ?? pvs[0].Id };
                return new LoginResultDTO { NextStep = AuthMessages.GetMessage(AuthResult.AllowAccess), User = userDto };
            }
        }

        public UsuarioLoginDTO ValidarUsuarioYObtenerDatos(string username, string passwordEnviada, string compId = null)
        {
            try
            {
                using (var conn = new SqlConnection(GetIdentityConn()))
                {
                    var cmd = new SqlCommand("SELECT Id, PasswordHash FROM AspNetUsers WHERE UserName = @user", conn);
                    cmd.Parameters.Add("@user", SqlDbType.NVarChar).Value = username.Trim();
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string identityId = reader["Id"].ToString();
                            string storedHash = reader["PasswordHash"]?.ToString().Trim();
                            var hasher = new PasswordHasher();
                            if (hasher.VerifyHashedPassword(storedHash, passwordEnviada) != PasswordVerificationResult.Failed)
                            {
                                var dto = ObtenerDatosPorNombre(username, identityId, compId);
                                if (dto != null) { dto.IdentityGuid = identityId; return dto; }
                            }
                        }
                    }
                }
                return null;
            }
            catch (Exception ex) { Log.Error(ex, AuthMessages.GetMessage(AuthResult.DatabaseErrorLog).Replace("{Username}", username)); return null; }
        }

        public UsuarioLoginDTO ObtenerDatosPorNombre(string username, string identityId, string compId = null)
        {
            try
            {
                UsuarioLoginDTO usuario = null;
                using (var conn = new SqlConnection(GetConnectionString(compId)))
                {
                    string query = @"SELECT e.CODIGO_BIC, LTRIM(RTRIM(b.NOMBRE_BIC)) + ' ' + LTRIM(RTRIM(b.APELLIDO_BIC)) AS Nombre, 
                                   e.ESTATUS_DEL_EMPLEADO, R.Name AS RolName
                                   FROM USUARIO_DEL_SISTEMA u
                                   INNER JOIN EMPLEADO_TABLA e ON u.CODIGO_BIC = e.CODIGO_BIC
                                   INNER JOIN BASE_INFO_CENTRAL b ON e.CODIGO_BIC = b.CODIGO_BIC
                                   LEFT JOIN [BIT_Identityv2].[dbo].[AspNetUserRoles] UR ON UR.UserId = @uid
                                   LEFT JOIN [BIT_Identityv2].[dbo].[AspNetRoles] R ON UR.RoleId = R.Id
                                   WHERE u.OS4SA = @user";
                    var cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@user", username.Trim());
                    cmd.Parameters.AddWithValue("@uid", identityId);
                    conn.Open();
                    using (var dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            usuario = new UsuarioLoginDTO
                            {
                                EmpleadoID = dr["CODIGO_BIC"].ToString().Trim(),
                                NombreCompleto = dr["Nombre"].ToString().Trim(),
                                Rol = dr["RolName"]?.ToString() ?? AuthMessages.GetMessage(AuthResult.GetCurrentUserError),
                                Estado = dr["ESTATUS_DEL_EMPLEADO"].ToString().Trim(),
                                CompaniaID = compId
                            };
                        }
                    }
                }
                return usuario;
            }
            catch { return null; }
        }
    }
}