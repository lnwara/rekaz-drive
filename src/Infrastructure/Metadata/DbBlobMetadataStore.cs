using Microsoft.EntityFrameworkCore;
using RekazDrive.Application.Abstractions;
using RekazDrive.Infrastructure.StorageDb;

namespace RekazDrive.Infrastructure.Metadata;

public sealed class DbBlobMetadataStore : IBlobMetadataStore
{
    private readonly BlobDbContext _db;
    public DbBlobMetadataStore(BlobDbContext db) => _db = db;

    public async Task UpsertAsync(string id, long size, DateTimeOffset createdAtUtc, CancellationToken ct = default)
    {
        var meta = await _db.BlobMetadata.FindAsync(new object?[] { id }, ct);
        if (meta is null)
        {
            meta = new BlobMetadata { Id = id, Size = size, CreatedAtUtc = createdAtUtc };
            _db.BlobMetadata.Add(meta);
        }
        else
        {
            meta.Size = size;
            _db.BlobMetadata.Update(meta);
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task<(string Id, long Size, DateTimeOffset CreatedAtUtc)?> GetAsync(string id, CancellationToken ct = default)
    {
        var meta = await _db.BlobMetadata.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (meta is null) return null;
        return (meta.Id, meta.Size, meta.CreatedAtUtc);
    }
}

