# Creating the infrastructure for this Azure WebApp

## Creating the Azure AAD SPNs

The app needs to SPN (Service Principals / App Registrations). One to read and write in the database, and to contact other services, for the actual app, the other is for migrations (it has ddladmin in the database and can create/remove tables, columns etc - which the normal SPN does not have).

1. Run `CreateCertificates.ps1` to create the certificates. It needs the MyWebApp CA certificate on your machine.
1. Export the certificates from your local store. Both a private `.pfx`, with the password from our operations KeyVault, and a public `.cer`
1. Run `CreateAdApplicationAndAssociateCertificate.ps1` with the new certificates to create the Service Principals

## Terraform and the infrastructure

Takes the SPN Ids and certificates as input

## Give the SPNs access to the database

Run `createSPN.sql` in the databases where the app should have access to read and write. Run `createMigratorSPN.sql` in the database where it should be allowed ti migrate