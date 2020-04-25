# azure-function-dotnet-gitcheck

Timer-triggered C# Azure Function which detects changes in a GitHub repositories list.
Uses Azure Blob Storage to maintain the state of the application.

## Links

- https://docs.microsoft.com/en-us/azure/azure-functions/functions-develop-vs-code?tabs=csharp

## GitHub Endpoint

You'll need a GitHub REST API token to invoke this endpoint:

```
curl -i https://api.github.com/users/<your-github-id>/repos
curl -i https://api.github.com/users/cjoakim/repos
```

## Packages

```
dotnet add package Azure.Storage.Blobs
dotnet add package System.Text.Json
```

## Environment Variables

Set these two in your Azure Function app:

```
GITHUB_REST_API_READ_TOKEN        <-- your GitHub token for invoking the REST API
AZURE_STORAGE_CONNECTION_STRING   <-- the connection string to your Azure Storage Account
```

## Implementation Code and Notes

See file **GitCheckCS.cs**
