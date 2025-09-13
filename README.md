# RekazDrive (Blob Storage API)

Minimal blob storage API in ASP.NET Core 8. Stores Base64 blobs and retrieves them by id. Storage backend is selectable by config (File System, Database, S3 via HTTP SigV4, FTP).

## 1) Requirements
- .NET SDK 8.x

## 2) Configure (defaults are fine for local)
Edit `src/WebApi/appsettings.json`:
- Auth: set `Auth.Username`, `Auth.Password`
- JWT: set a strong `Jwt.SigningKey`
- Storage: choose `Storage.Provider` = `FileSystem` | `Database` | `S3` | `Ftp`

## 3) Run
```
dotnet restore RekazDrive.sln
dotnet build -c Debug RekazDrive.sln
dotnet run --project src/WebApi --urls http://localhost:5000
```

Open Swagger: http://localhost:5000/swagger

## 4) Authenticate
- Login: `POST /v1/auth/login` with `{ "username": "admin", "password": "admin" }`
- Copy `access_token` and click “Authorize” in Swagger (Bearer <token>)

## 5) APIs
- Create blob
  - `POST /v1/blobs`
  - Body: `{ "data": "<base64>" }`
  - Response: `201` `{ id, size, created_at }` (id is server-generated)
- Get blob
  - `GET /v1/blobs/{id}` → `{ id, data, size, created_at }`

## 6) Storage Backends (appsettings.json)
- FileSystem: `Storage.FileSystem.Root` (created if missing)
- Database (SQLite): `ConnectionStrings.BlobDb` (migrations auto-applied)
- S3 (no SDK): `Storage.S3.{Bucket,EndpointHost,Region,AccessKey,SecretKey,UsePathStyle}`
- FTP: `Storage.Ftp.{Host,Username,Password}`

## 7) Tests
```
dotnet test tests/RekazDrive.Application.Tests/RekazDrive.Application.Tests.csproj -c Debug
```

