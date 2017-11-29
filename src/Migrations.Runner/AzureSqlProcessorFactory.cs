using System.Data.SqlClient;
using System.Security.Cryptography.X509Certificates;
using FluentMigrator;
using FluentMigrator.Runner;
using FluentMigrator.Runner.Generators.SqlServer;
using FluentMigrator.Runner.Processors;
using FluentMigrator.Runner.Processors.SqlServer;

namespace MyAppMigrations.Runner
{
    internal class AzureSqlProcessorFactory : MigrationProcessorFactory
    {
        private readonly string _clientId;
        private readonly string _certThumbPrint;
        private readonly string _authority;

        public AzureSqlProcessorFactory(string clientId, string certThumbPrint, string authority)
        {
            _clientId = clientId;
            _certThumbPrint = certThumbPrint;
            _authority = authority;
        }

        public override IMigrationProcessor Create(string connectionString, IAnnouncer announcer, IMigrationProcessorOptions options)
        {
            var factory = new SqlServerDbFactory();
            
            var accessTokenFactory = new AccessTokenFactory(_clientId, _certThumbPrint, StoreLocation.CurrentUser);
            var accessToken = accessTokenFactory.GetTokenAsync(_authority, "https://database.windows.net/").GetAwaiter().GetResult();

            var connection = new SqlConnection(connectionString) { AccessToken = accessToken };
            
            return new SqlServerProcessor(connection, new SqlServer2014Generator(), announcer, options, factory);
        }
    }
}