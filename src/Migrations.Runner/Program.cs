using CLAP;

namespace MyAppMigrations.Runner
{
    class Program
    {

        [Verb(IsDefault = true)]
        static void Migrate(
            [Required] string connectionString,
            [Required] string clientId,
            [Required] string certThumbPrint,
            [Required] string authority)
        {
            Runner.MigrateToLatest(connectionString, clientId, certThumbPrint, authority);
        }

        [Help]
        [Empty]
        static void ShowHelp(string help)
        {
        }

        static void Main(string[] args)
        {
            Parser.Run<Program>(args);
        }
    }
}
