using System;
using System.IO;
using System.Threading.Tasks;
using DbUp;
using DbUp.Engine;
using DbUp.ScriptProviders;
using Serilog;
using Serilog.Formatting.Compact;

namespace DatabaseMigrator
{
    internal class Program
    {
        private static async Task<int> Main(string[] args)
        {
            var logsPath = "logs\\migration-logs";

            ILogger logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.RollingFile(new CompactJsonFormatter(), logsPath)
                .CreateLogger();

            logger.Information("Logger configured. Starting migration...");

            if (args.Length != 3)
            {
                logger.Error("Invalid arguments. Execution: DatabaseMigrator [connectionString] [pathToScripts] [timeout].");

                logger.Information("Migration stopped");

                return -1;
            }

            string connectionString = args[0];

            string scriptsPath = args[1];

            string timeoutArg = args[2];

            if (!Directory.Exists(scriptsPath))
            {
                logger.Information($"Directory {scriptsPath} does not exist");

                return -1;
            }

            if (!int.TryParse(timeoutArg, out int timeout))
            {
                logger.Information($"Timeout {timeoutArg} is not a valid integer");

                return -1;
            }

            var serilogUpgradeLog = new SerilogUpgradeLog(logger);

            UpgradeEngine upgradeEngine =
                DeployChanges.To
                    .SqlDatabase(connectionString)
                    .WithExecutionTimeout(TimeSpan.FromSeconds(timeout))
                    .WithScriptsFromFileSystem(scriptsPath, new FileSystemScriptOptions
                    {
                        IncludeSubDirectories = true
                    })
                    .LogTo(serilogUpgradeLog)
                    .JournalToSqlTable("app", "MigrationsJournal")
                    .Build();

            DatabaseUpgradeResult result = null;
            DateTime dateTimeTimeout = DateTime.Now.AddSeconds(timeout);
            while (result is null && DateTime.Now < dateTimeTimeout)
            {
                try
                {
                    result = upgradeEngine.PerformUpgrade();
                }
                catch (NullReferenceException)
                {
                    // Throw NullReferenceException when database not available yet
                    const int retryDelay = 5;
                    logger.Information($"Unable to connect to database, retry in {retryDelay} seconds.");
                    await Task.Delay(TimeSpan.FromSeconds(retryDelay));
                }
            }


            if (result is null || !result.Successful)
            {
                logger.Information("Migration failed");

                return -1;
            }

            logger.Information("Migration successful");

            return 0;
        }
    }
}