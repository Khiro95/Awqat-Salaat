using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;
using System.IO;

namespace AwqatSalaat.Helpers
{
    public static class LogManager
    {
        private static bool s_loggerCreated;
        private static readonly LoggingLevelSwitch s_levelSwitch = new LoggingLevelSwitch();

        public static string WidgetIdentity;
        public static Action<LoggerConfiguration> LoggerConfigurationHandler;
        public static readonly string LogsPath = Path.Combine(Path.GetTempPath(), "awqat_salaat.log");

        public static void InvalidateLogger()
        {
            var settings = Properties.Settings.Default;

            if (settings.EnableLogs)
            {
                s_levelSwitch.MinimumLevel = settings.EnableDebugLogs ? LogEventLevel.Debug : LogEventLevel.Information;

                if (!s_loggerCreated)
                {
                    var config = new LoggerConfiguration()
                        .WriteTo.File(LogsPath)
                        .MinimumLevel.ControlledBy(s_levelSwitch);

                    LoggerConfigurationHandler?.Invoke(config);

                    Log.Logger = config.CreateLogger();

                    Log.Information(WidgetIdentity);
                    Log.Information($"Windows build: {SystemInfos.OSBuildNumber}");

                    s_loggerCreated = true;
                }
            }
            else if (s_loggerCreated)
            {
                Log.Information("Stopping logger");
                Log.CloseAndFlush();
                s_loggerCreated = false;
            }
        }
    }
}
