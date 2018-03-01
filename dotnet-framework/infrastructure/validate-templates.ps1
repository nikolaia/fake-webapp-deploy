az group deployment validate -g MyApp.Dev --template-file "arm/azuredeploy.json" --parameters @arm/parameters/dev.parameters.json
az group deployment validate -g MyApp.Test --template-file "arm/azuredeploy.json" --parameters @arm/parameters/test.parameters.json
az group deployment validate -g MyApp --template-file "arm/azuredeploy.json" --parameters @arm/parameters/production.parameters.json