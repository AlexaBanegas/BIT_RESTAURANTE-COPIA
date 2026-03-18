using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using BIT_PRUEBA_BE.Enums;
using Serilog;

namespace BIT_PRUEBA_BE.Data
{
    public class RefreshTokenData
    {
        private readonly string cnn = ConfigurationManager.ConnectionStrings["BIT_Identity_Conn"].ConnectionString;

        public bool ValidarYRotar(string tokenRecibido, string ipAddress, out string userIdGuid, out int oldTokenId)
        {
            userIdGuid = null;
            oldTokenId = 0;

            using (SqlConnection conn = new SqlConnection(cnn))
            {
                conn.Open();
                using (SqlTransaction trans = conn.BeginTransaction())
                {
                    try
                    {
                        string query = @"SELECT Id, UserId, IsUsed, IsRevoked, Expiration 
                                       FROM RefreshTokens WHERE Token = @token";

                        SqlCommand cmd = new SqlCommand(query, conn, trans);
                        cmd.Parameters.AddWithValue("@token", tokenRecibido);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (!reader.Read()) return false;

                            oldTokenId = reader.GetInt32(reader.GetOrdinal("Id"));
                            userIdGuid = reader.GetString(reader.GetOrdinal("UserId"));
                            bool isUsed = reader.GetBoolean(reader.GetOrdinal("IsUsed"));
                            bool isRevoked = reader.GetBoolean(reader.GetOrdinal("IsRevoked"));
                            DateTime expiration = reader.GetDateTime(reader.GetOrdinal("Expiration"));

                            if (isUsed || isRevoked || expiration < DateTime.Now.AddSeconds(-10)) return false;
                        }

                        string updateQuery = "UPDATE RefreshTokens SET IsUsed = 1 WHERE Id = @id";
                        SqlCommand upCmd = new SqlCommand(updateQuery, conn, trans);
                        upCmd.Parameters.AddWithValue("@id", oldTokenId);
                        upCmd.ExecuteNonQuery();

                        trans.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        trans.Rollback();
                        Log.Error(ex, AuthMessages.GetMessage(AuthResult.DatabaseErrorLog).Replace("{Username}", "RotationError"));
                        return false;
                    }
                }
            }
        }

        public void RegistrarNuevoToken(string token, string userIdGuid, string ip, int? replacedById = null)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(cnn))
                {
                    string query = @"INSERT INTO RefreshTokens (Token, UserId, Expiration, IsUsed, IsRevoked, CreatedByIp, ReplacedByTokenId) 
                                   VALUES (@t, @u, @e, 0, 0, @ip, @r)";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@t", token);
                    cmd.Parameters.AddWithValue("@u", userIdGuid);
                    cmd.Parameters.AddWithValue("@e", DateTime.Now.AddMinutes(10));
                    cmd.Parameters.AddWithValue("@ip", (object)ip ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@r", (object)replacedById ?? DBNull.Value);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, AuthMessages.GetMessage(AuthResult.DatabaseErrorLog).Replace("{Username}", userIdGuid));
            }
        }
    }
}