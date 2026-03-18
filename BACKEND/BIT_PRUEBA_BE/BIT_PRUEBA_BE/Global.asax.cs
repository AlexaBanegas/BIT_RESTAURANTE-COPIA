using BIT_PRUEBA_BE.Enums;
using Serilog;
using Serilog.Context;
using Serilog.Sinks.MSSqlServer;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data;
using System.Web;
using System.Web.Http;

namespace BIT_PRUEBA_BE
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
           

            string connectionString = ConfigurationManager.ConnectionStrings["BIT_FCH_Conn"].ConnectionString;

            var columnOptions = new ColumnOptions();
            columnOptions.Store.Remove(StandardColumn.Properties);
            columnOptions.Store.Remove(StandardColumn.MessageTemplate);
            columnOptions.AdditionalColumns = new Collection<SqlColumn>
            {
                new SqlColumn { ColumnName = "UserName", DataType = SqlDbType.NVarChar, DataLength = 100, AllowNull = true }
            };

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.FromLogContext()
                .WriteTo.File(@"C:\BIT_GRUB_Logs\log-.txt",
                    rollingInterval: RollingInterval.Day,
                    shared: true,
                    flushToDiskInterval: TimeSpan.FromSeconds(1))
                .WriteTo.MSSqlServer(
                    connectionString: connectionString,
                    sinkOptions: new MSSqlServerSinkOptions
                    {
                        TableName = "UserLogs",
                        AutoCreateSqlTable = false,
                        BatchPostingLimit = 1
                    },
                    columnOptions: columnOptions)
                .CreateLogger();

            using (LogContext.PushProperty("UserName", "SISTEMA"))
            {
                Log.Information(AuthMessages.GetMessage(AuthResult.SystemStartHeader));
                Log.Information(AuthMessages.GetMessage(AuthResult.SystemStartMessage));
                Log.Information(AuthMessages.GetMessage(AuthResult.SystemStartSeparator));
            }
        }

        protected void Application_End()
        {
            Log.CloseAndFlush();
        }
    }
}