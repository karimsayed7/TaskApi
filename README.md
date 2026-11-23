# TaskApi

## Local configuration

The API requires a SQL Server connection string and a Base64-encoded JWT signing key of at least 32 bytes. Keep both values in User Secrets or environment variables; do not commit them to `appsettings*.json`.

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<your SQL Server connection string>" --project src\TaskApi.Api\TaskApi.Api.csproj
dotnet user-secrets set "Jwt:SigningKey" "<Base64-encoded key with at least 32 bytes>" --project src\TaskApi.Api\TaskApi.Api.csproj
```

Generate a signing key in PowerShell with:

```powershell
[Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
```

The layered API uses `TaskApi.Infrastructure.Persistence.TaskApiDbContext`; its migrations belong to the Infrastructure project.
