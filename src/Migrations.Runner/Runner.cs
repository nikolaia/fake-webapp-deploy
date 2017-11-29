using System.Reflection;
using FluentMigrator;
using FluentMigrator.Runner;
using FluentMigrator.Runner.Announcers;
using FluentMigrator.Runner.Initialization;

namespace MyAppMigrations.Runner
{
    internal static class Runner
    {
        public class MigrationOptions : IMigrationProcessorOptions
        {
            public bool PreviewOnly { get; set; }
            public string ProviderSwitches { get; set; }
            public int Timeout { get; set; }
        }

        public static void MigrateToLatest(string connectionString, string clientId, string certThumbPrint, string authority)
        {
            var announcer = new TextWriterAnnouncer(s => { 
                System.Console.WriteLine(s);
                System.Diagnostics.Debug.WriteLine(s);
            });
            var assembly = Assembly.Load("MyAppMigrations");

            var migrationContext = new RunnerContext(announcer)
            {
                Namespace = "MyAppMigrations"
            };

            var options = new MigrationOptions { PreviewOnly = false, Timeout = 60 };
            var factory = new AzureSqlProcessorFactory(clientId, certThumbPrint, authority);

            using (var processor = factory.Create(connectionString, announcer, options))
            {
                var runner = new MigrationRunner(assembly, migrationContext, processor);
                runner.ListMigrations();
                runner.MigrateUp(true);
            }
        }
    }
}