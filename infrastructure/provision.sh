#!/usr/bin/env bash

set -e

# Install Azure CLI 2.0: curl -L https://aka.ms/InstallAzureCli | bash
# Login: az login
# sudo apt-get install jq  # <- for JSON Parsing

: ${dbpass?"Need to set variable dbpass"} # ex: 8be7AzZuty*R9zdQMuPX2jfs&%Y22Z
: ${number?"Need to set variable number"} # ex: export number=$RANDOM
: ${name?"Need to set variable number"} # ex: export name=testing

webappname="${name}${number}"
resourceGroupName="${webappname}"
location="West Europe"
keyvaultName="${webappname}-kv"
spnName="${webappname}-spn"
certName="${spnName}-cert"
sqlName="${name}${number}sql"
dbName="${name}${number}"

tenantId=$(az account show | jq -r '.id')

echo "Number = ${number}"
echo "WebappName = ${webappname}"
echo "Location = ${location}"
echo "KeyvaultName = ${keyvaultName}"
echo "spnName = ${spnName}"

# Create Resource Group
echo "===================== CREATING RESOURCE GROUP ===================== "
az group create --name $resourceGroupName --location "$location"

# Create KeyVault
echo "===================== CREATING KEYVAULT ===================== "
az keyvault create \
    --name $keyvaultName \
    --resource-group $resourceGroupName \
    --location "$location" \
    --enabled-for-template-deployment true

# Create Service Principle 
echo "===================== CREATING SPN ===================== "
set +e
cert=$(az ad sp create-for-rbac -n $spnName --keyvault $keyvaultName --cert $certName --create-cert)
set -e
echo $cert
spnId=$(az ad sp list --spn "http://${spnName}" | jq -r '.[0].appId')
echo "spnId = ${spnId}"
# Example output:   
# {
#   "appId": "ac62763a-ad46-46b6-91d9-016bf7741256",
#   "displayName": "MyBTerraform",
#   "name": "http://MyBTerraform",
#   "password": null,
#   "tenant": "de7e7a67-ae61-49d2-97a7-526c910ad675"
# }

echo "===================== CREATING APPSERVICE PLAN ===================== "
az appservice plan create --name $webappname --location "$location" --resource-group $resourceGroupName --sku S1

echo "===================== CREATING WEBAPP ===================== "
az webapp create --name $webappname --resource-group $resourceGroupName --plan $webappname

echo "===================== CREATING SQL SERVER ====================="
az sql server create -u mybWeb -p $dbpass --location "$location" -n $sqlName -g $resourceGroupName

echo "===================== CREATING SQL DATABASE INSTANCE ====================="
az sql db create -n $dbName -g $resourceGroupName -s $sqlName

echo "===================== ENABLING WEBAPP MANAGED IDENTITY - MSI -  ====================="
# https://docs.microsoft.com/en-us/azure/app-service/app-service-managed-service-identity
set +e
az webapp assign-identity --resource-group $resourceGroupName --name $webappname --role reader --scope "/subscriptions/${tenantId}/${resourceGroupName}"
set -e
principalId=$(az webapp list --query "[?name=='${webappname}']" | jq -r '.[0].identity.principalId')
echo "principalId = ${principalId}"

# TODO: Give the spnId read and write access in the database
# TODO: Give the principalId read access of the certificate in the keyvault store
# TODO: Change AzureClient to load the certificate from the keyvault

echo "===================== CREATING SQL SERVER AD ADMIN ====================="
# This makes the WebApp Managed identity admin of the database so it can do migrations on deploy
az sql server ad-admin create -u $webappname -i $principalId -g $resourceGroupName -s $sqlName

echo "===================== INFORMATION ====================="
echo "The Service Principal is not removed by removing the resource group! To delete everything use the following command:"
echo "az group delete -n $resourceGroupName && az ad sp delete --id ${spnId}"