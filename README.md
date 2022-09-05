# Demo SQL Insert Azure Function

## Running the application

### Set up local settings

 - Ensure you have [Azure functions core tools](https://docs.microsoft.com/en-us/azure/azure-functions/functions-run-local?tabs=v4%2Cwindows%2Ccsharp%2Cportal%2Cbash) v4 installed
 - Create a local.settings.json file in the Demo.SqlInsertFunc project
 - Set up your Azure Storage connection in the local.settings.json file
   - This can be `UseDevelopmentStorage=true` if you have the storage emulator running
 - Set up your SQL connection string, named `SqlConnectionString`
 - Ensure your SQL database has the necessary schemas and tables - reference the models in this project

### Build & Run

```[bash]
dotnet build
func start
```

### Test

```[bash]
dotnet test
```

## Warnings

The `[Sql]` attribute works with a `MERGE` statement, so if something does end up having the same ID through a coding error or other bug, then it will erroneously update a previously inserted record. 