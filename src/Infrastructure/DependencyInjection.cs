using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using RekazDrive.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using RekazDrive.Infrastructure.Metadata;
using RekazDrive.Infrastructure.Storage;
using RekazDrive.Infrastructure.StorageDb;

namespace RekazDrive.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddBlobInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var conn = config.GetConnectionString("BlobDb") ?? "Data Source=App_Data/blobs.db";
        try
        {
            var parts = conn.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
            var dsIndex = parts.FindIndex(p => p.TrimStart().StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase));
            if (dsIndex >= 0)
            {
                var rawPath = parts[dsIndex].Split('=', 2)[1].Trim();
                var fullPath = Path.IsPathRooted(rawPath) ? rawPath : Path.Combine(AppContext.BaseDirectory, rawPath);
                var dir = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                parts[dsIndex] = $"Data Source={fullPath}";
                conn = string.Join(';', parts);
            }
        }
        catch { }
        services.AddDbContext<BlobDbContext>(o => o.UseSqlite(conn));
        services.AddScoped<IBlobMetadataStore, DbBlobMetadataStore>();

        // Choose content backend
        var provider = config["Storage:Provider"]?.ToLowerInvariant() ?? "filesystem";
        services.AddHttpClient();
        services.AddScoped<IBlobStorage>(sp =>
        {
            switch (provider)
            {
                case "filesystem":
                default:
                {
                    var configuredRoot = config["Storage:FileSystem:Root"];
                    string root = string.IsNullOrWhiteSpace(configuredRoot)
                        ? Path.Combine(AppContext.BaseDirectory, "App_Data", "blobs")
                        : (Path.IsPathRooted(configuredRoot)
                            ? configuredRoot
                            : Path.Combine(AppContext.BaseDirectory, configuredRoot));
                    return new FileSystemBlobStorage(root);
                }
                case "database":
                    return new DatabaseBlobStorage(sp.GetRequiredService<BlobDbContext>());
                case "s3":
                {
                    var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
                    var bucket = config["Storage:S3:Bucket"] ?? throw new InvalidOperationException("Storage:S3:Bucket is required");
                    var host = config["Storage:S3:EndpointHost"] ?? "s3.amazonaws.com";
                    var region = config["Storage:S3:Region"] ?? "us-east-1";
                    var access = config["Storage:S3:AccessKey"] ?? throw new InvalidOperationException("Storage:S3:AccessKey is required");
                    var secret = config["Storage:S3:SecretKey"] ?? throw new InvalidOperationException("Storage:S3:SecretKey is required");
                    var pathStyle = bool.TryParse(config["Storage:S3:UsePathStyle"], out var ps) && ps;
                    return new S3HttpBlobStorage(http, bucket, host, region, access, secret, pathStyle);
                }
                case "ftp":
                {
                    var host = config["Storage:Ftp:Host"] ?? throw new InvalidOperationException("Storage:Ftp:Host is required");
                    var user = config["Storage:Ftp:Username"] ?? throw new InvalidOperationException("Storage:Ftp:Username is required");
                    var pass = config["Storage:Ftp:Password"] ?? throw new InvalidOperationException("Storage:Ftp:Password is required");
                    return new FtpBlobStorage(host, user, pass);
                }
            }
        });

        return services;
    }
}
