using System.Buffers.Text;
using System.Text;
using RekazDrive.Application.Abstractions;

namespace RekazDrive.Application.Services;

public sealed class BlobService
{
    private readonly IBlobStorage _storage;
    private readonly IBlobMetadataStore _meta;

    public BlobService(IBlobStorage storage, IBlobMetadataStore meta)
    {
        _storage = storage;
        _meta = meta;
    }

    public async Task<(string Id, long Size, DateTimeOffset CreatedAt)> StoreAsync(string base64Data, CancellationToken ct = default)
    {
        var id = Guid.NewGuid().ToString("N");
        if (!TryDecodeBase64(base64Data, out var bytes))
            throw new FormatException("Invalid base64 data");

        await _storage.SaveAsync(id, bytes, ct);
        var now = DateTimeOffset.UtcNow;
        await _meta.UpsertAsync(id, bytes.Length, now, ct);
        return (id, bytes.Length, now);
    }

    public async Task<BlobResult> RetrieveAsync(string id, CancellationToken ct = default)
    {
        var meta = await _meta.GetAsync(id, ct) ?? throw new KeyNotFoundException("Blob not found");
        var bytes = await _storage.GetAsync(id, ct);
        var b64 = Convert.ToBase64String(bytes.Span);
        return new BlobResult(meta.Id, b64, meta.Size, meta.CreatedAtUtc);
    }

    private static bool TryDecodeBase64(string input, out ReadOnlyMemory<byte> result)
    {
        try
        {
            var data = Convert.FromBase64String(input);
            result = new ReadOnlyMemory<byte>(data);
            return true;
        }
        catch
        {
            result = ReadOnlyMemory<byte>.Empty;
            return false;
        }
    }

    public sealed record BlobResult(string Id, string Data, long Size, DateTimeOffset CreatedAt);
}
