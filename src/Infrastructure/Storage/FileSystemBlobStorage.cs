using System.Security.Cryptography;
using RekazDrive.Application.Abstractions;

namespace RekazDrive.Infrastructure.Storage;

public sealed class FileSystemBlobStorage : IBlobStorage
{
    private readonly string _root;

    public FileSystemBlobStorage(string root)
    {
        _root = root;
        Directory.CreateDirectory(_root);
    }

    public Task<bool> ExistsAsync(string id, CancellationToken ct = default)
        => Task.FromResult(File.Exists(MapPath(id)));

    public async Task<ReadOnlyMemory<byte>> GetAsync(string id, CancellationToken ct = default)
    {
        var path = MapPath(id);
        if (!File.Exists(path)) throw new FileNotFoundException("Blob not found", id);
        var data = await File.ReadAllBytesAsync(path, ct);
        return new ReadOnlyMemory<byte>(data);
    }

    public async Task SaveAsync(string id, ReadOnlyMemory<byte> data, CancellationToken ct = default)
    {
        var path = MapPath(id, createDirs: true);
        await File.WriteAllBytesAsync(path, data.ToArray(), ct);
    }

    public Task DeleteAsync(string id, CancellationToken ct = default)
    {
        var path = MapPath(id);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    private string MapPath(string id, bool createDirs = false)
    {
        // Allow ids that can look like paths; sanitize to prevent escaping root
        var parts = id.Replace("\\", "/").Split('/', StringSplitOptions.RemoveEmptyEntries);
        var safeParts = parts.Select(p => p.Replace("..", string.Empty)).ToArray();
        var full = Path.Combine(new[] { _root }.Concat(safeParts).ToArray());
        var fullDir = Path.GetDirectoryName(full);
        if (createDirs && !string.IsNullOrEmpty(fullDir)) Directory.CreateDirectory(fullDir);
        var normalizedRoot = Path.GetFullPath(_root) + Path.DirectorySeparatorChar;
        var normalized = Path.GetFullPath(full);
        if (!normalized.StartsWith(normalizedRoot, StringComparison.Ordinal))
            throw new InvalidOperationException("Invalid id path");
        return normalized;
    }
}

