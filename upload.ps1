Param(
    [Parameter(Mandatory=$True,Position=1)]
    [string]$appName
  )

$token = (ConvertFrom-Json -InputObject ([string](Invoke-Expression -Command:"az account get-access-token")))."accessToken"
$bearerToken = "Bearer $token"
$apiUrl = "https://$appName.scm.azurewebsites.net/api/zipdeploy"

$vstsWorkDir = $env:SYSTEM_DEFAULTWORKINGDIRECTORY
$vstsReleaseDefName = $env:RELEASE_DEFINITIONNAME

$artifactDir = if ($vstsWorkDir -and $vstsReleaseDefName) { 
        "$($env:SYSTEM_DEFAULTWORKINGDIRECTORY)/$($env:RELEASE_DEFINITIONNAME)/drop"
    } else { 
        split-path -parent $MyInvocation.MyCommand.Definition
    }

$zip = Get-ChildItem -Path $artifactDir -Filter *.zip | Select-Object -First 1
$zipPath = "$artifactDir/$($zip.Name)"
Write-Host "Calling $apiUrl with $zipPath using token '$bearerToken'"
Invoke-RestMethod -Uri $apiUrl -Headers @{Authorization=$bearerToken} -Method POST -InFile $zipPath -ContentType "multipart/form-data"