# Deploy

```powershell
az group deployment create -g MyApp.Test --template-file "arm/azuredeploy.json" --parameters @arm/parameters/dev.parameters.json
```