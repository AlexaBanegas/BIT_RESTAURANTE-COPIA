using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using BIT_PRUEBA_BE.Enums;
using Serilog;

namespace BIT_PRUEBA_BE.Data
{
    public class MesaDTO
    {
        public int IdMesa { get; set; }
        public int Numero { get; set; }
        public int Capacidad { get; set; }
        public string Estado { get; set; }
    }

    public class TableStatusUpdateDTO
    {
        public int NuevoEstado { get; set; }
    }

    public class TableData
    {
        public List<MesaDTO> ListarMesas(string codigoRestaurante, string connectionString)
        {
            List<MesaDTO> lista = new List<MesaDTO>();
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    string query = @"SELECT DISTINCT CODIGO_MESA, CAPACIDAD_PERSONAS, ESTADOS 
                                   FROM RESTAURANTES_MESAS 
                                   WHERE CODIGO_RESTAURANTE = @pv";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@pv", codigoRestaurante);
                    conn.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            int id = Convert.ToInt32(dr["CODIGO_MESA"]);
                            lista.Add(new MesaDTO
                            {
                                IdMesa = id,
                                Numero = id,
                                Capacidad = Convert.ToInt32(dr["CAPACIDAD_PERSONAS"]),
                                Estado = dr["ESTADOS"].ToString().Trim()
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, AuthMessages.GetMessage(AuthResult.ErrorLogDatabase));
            }
            return lista;
        }

        public bool ActualizarEstado(int id, int nuevoEstado, string codigoRestaurante, string connectionString)
        {
            try
            {
                string valorEstado = (nuevoEstado == 1) ? "1" : "2";

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    string query = @"UPDATE RESTAURANTES_MESAS 
                                   SET ESTADOS = @estado 
                                   WHERE CODIGO_MESA = @id AND CODIGO_RESTAURANTE = @pv";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.Add("@estado", SqlDbType.Char, 1).Value = valorEstado;
                    cmd.Parameters.Add("@id", SqlDbType.Int).Value = id;
                    cmd.Parameters.AddWithValue("@pv", codigoRestaurante);

                    conn.Open();
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, AuthMessages.GetMessage(AuthResult.ErrorLogDatabase) + " ID: {Id}", id);
                return false;
            }
        }
    }
}