using System;

namespace BIT_PRUEBA_BE.Enums
{
    public enum AuthResult
    {
        // FORMULARIO DE LOGIN 
        Success,
        UserInactive,
        InvalidCredentials,
        IncompleteData,
        TokenExpired,

        // AUTHCONTROLLER.CS 
        LogProcessing,
        LogInvalidCreds,
        LogUserInactive,
        LogAccessGranted,
        GetCurrentUserSuccess,
        GetCurrentUserError,
        LogProfileAccess,

        // GLOBAL.ASAX 
        SystemStartHeader,
        SystemStartMessage,
        SystemStartSeparator,

        // USUARIODATA.CS 
        DatabaseErrorLog,

        // TABLEDATA.CS / TABLESCONTROLLER.CS
        MesaDisponible,
        MesaOcupada,
        ErrorCargarMesas,
        ErrorUpdateMesa,
        UpdateSuccess,
        ErrorLogDatabase,
        TableListSuccess, 
        TableNotFound,
        SelectCompany,
        SelectRestaurant,
        AllowAccess


    }

    public enum UserStatus
    {
        Activo,
        Inactivo
    }

    public static class AuthMessages
    {
        public static string GetMessage(AuthResult result)
        {
            switch (result)
            {
                // FORMULARIO DE LOGIN 
                case AuthResult.Success:
                    return "Inicio de sesión exitoso.";
                case AuthResult.UserInactive:
                    return "No tiene acceso al sistema. Su cuenta está inactiva.";
                case AuthResult.InvalidCredentials:
                    return "Usuario o contraseña no coinciden en nuestros registros.";
                case AuthResult.IncompleteData:
                    return "Datos incompletos.";
                case AuthResult.TokenExpired:
                    return "Su Token a Expirado. Inicie sesión de nuevo.";

                // AUTHCONTROLLER.CS 
                case AuthResult.LogProcessing:
                    return "Procesando intento de login para: {UserName}";
                case AuthResult.LogInvalidCreds:
                    return "ACCESO DENEGADO: Credenciales incorrectas para: {UserName}";
                case AuthResult.LogUserInactive:
                    return "ACCESO BLOQUEADO: El usuario {UserName} intentó entrar pero está INACTIVO";
                case AuthResult.LogAccessGranted:
                    return "ACCESO CONCEDIDO: Generando JWT para {UserName} ({Nombre}) con rol {Rol}";
                case AuthResult.GetCurrentUserSuccess:
                    return "Datos del usuario obtenidos correctamente.";
                case AuthResult.GetCurrentUserError:
                    return "No se pudo identificar al usuario conectado.";
                case AuthResult.LogProfileAccess:
                    return "PERFIL: El usuario {username} con rol {role} cargó su información en el Dashboard.";



                //GLOBAL.ASAX 
                case AuthResult.SystemStartHeader:
                    return "====================================================";
                case AuthResult.SystemStartMessage:
                    return "BIT Restaurante - BACKEND INICIADO Y ESCUCHANDO";
                case AuthResult.SystemStartSeparator:
                    return "====================================================";

                // USUARIODATA.CS 
                case AuthResult.DatabaseErrorLog:
                    return "Error crítico en base de datos al validar usuario: {Username}";

                // TABLEDATA.CS / TABLESCONTROLLER.CS
               
                case AuthResult.MesaDisponible:
                    return "Activo"; 
                case AuthResult.MesaOcupada:
                    return "Inactivo";
                case AuthResult.ErrorCargarMesas:
                    return "Error al cargar mesas";
                case AuthResult.ErrorUpdateMesa:
                    return "Error al actualizar estado";
                case AuthResult.UpdateSuccess:
                    return "Estado de mesa {Id} se cambio a {EstadoNombre} con exito";
                case AuthResult.ErrorLogDatabase:
                    return "Error crítico en base de datos al procesar mesas.";
                case AuthResult.TableListSuccess:
                    return "Consulta de mesas exitosa. Total: {0}";
                case AuthResult.TableNotFound:
                    return "No se encontró la mesa con ID: {0}";
                case AuthResult.SelectCompany:
                    return "SELECT_COMPANY";
                case AuthResult.SelectRestaurant:
                    return "SELECT_RESTAURANT";
                case AuthResult.AllowAccess:
                    return "ALLOW_ACCESS";

                default:
                    return "Operación no definida.";
            }
        }
    }
}