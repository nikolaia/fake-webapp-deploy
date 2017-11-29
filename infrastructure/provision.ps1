# ATest
# Set-AzureRmContext -SubscriptionName "SDSWP00 Waypoint Test"
# .\Provision-Web.ps1 -TemplateParametersFile '..\Templates\Web.atest.json' -ResourceGroupName 'MyWebApp.ATest'

# Production
# Set-AzureRmContext -SubscriptionName "SDSWP00 Waypoint Prod"
# .\Provision-Web.ps1 -TemplateParametersFile '..\Templates\Web.production.json' -ResourceGroupName 'MyWebApp'

#Requires -Version 3.0
#Requires -Module AzureRM.Resources
#Requires -Module Azure.Storage

Param(
    [string] $ResourceGroupName = 'MyWebApp.STest',
    [string] $TemplateFile = '..\Templates\Web.json',
    [string] $TemplateParametersFile = '..\Templates\Web.stest.json'
)

Import-Module Azure -ErrorAction SilentlyContinue

try {
    [Microsoft.Azure.Common.Authentication.AzureSession]::ClientFactory.AddUserAgent("VSAzureTools-HostInCloud$($host.name)".replace(" ","_"), "2.8")
} catch { }

Set-StrictMode -Version 3

$OptionalParameters = New-Object -TypeName Hashtable
$TemplateFile = [System.IO.Path]::Combine($PSScriptRoot, $TemplateFile)
$TemplateParametersFile = [System.IO.Path]::Combine($PSScriptRoot, $TemplateParametersFile)

New-AzureRmResourceGroupDeployment -Name ((Get-ChildItem $TemplateFile).BaseName + '-' + ((Get-Date).ToUniversalTime()).ToString('MMdd-HHmm')) `
                                   -ResourceGroupName $ResourceGroupName `
                                   -TemplateFile $TemplateFile `
                                   -TemplateParameterFile $TemplateParametersFile `
                                   @OptionalParameters `
                                   -Force -Verbose

# Manual follow-up: Use Azure Portal to gave user MyWebApp Test Depoly contributor access to the app service resource
