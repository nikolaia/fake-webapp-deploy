-- Ref http://stackoverflow.com/questions/2777422/in-sql-azure-how-what-script-can-i-use-to-create-a-read-only-user
use MyWebAppSTestWeb
CREATE USER [MyWebApp Test Web API Migrator] FROM EXTERNAL PROVIDER;
EXEC sp_addrolemember N'db_ddladmin', N'MyWebApp Test Web API Migrator'
EXEC sp_addrolemember N'db_datareader', N'MyWebApp Test Web API Migrator'
EXEC sp_addrolemember N'db_datawriter', N'MyWebApp Test Web API Migrator'
# Username is Display Name of AAD App