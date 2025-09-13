using Microsoft.EntityFrameworkCore;
using RekazDrive.Application.Abstractions;
using RekazDrive.Infrastructure.StorageDb;

namespace RekazDrive.Infrastructure.Storage;

public sealed class DatabaseBlobStorage : IBlobStorage
{
    private readonly BlobDbContext _db;
    public DatabaseBlobStorage(BlobDbContext db) => _db = db;

    public async Task SaveAsync(string id, ReadOnlyMemory<byte> data, CancellationToken ct = default)
    {
        var entity = await _db.BlobContents.FindAsync( id , ct);
        if (entity is null)
        {
            entity = new BlobContent { Id = id, Data = data.ToArray() };
            _db.BlobContents.Add(entity);
        }
        else
        {
            entity.Data = data.ToArray();
            _db.BlobContents.Update(entity);
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task<ReadOnlyMemory<byte>> GetAsync(string id, CancellationToken ct = default)
    {
        var entity = await _db.BlobContents.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) throw new FileNotFoundException("Blob not found", id);
        return new ReadOnlyMemory<byte>(entity.Data ?? Array.Empty<byte>());
    }

    public async Task<bool> ExistsAsync(string id, CancellationToken ct = default)
        => await _db.BlobContents.AnyAsync(x => x.Id == id, ct);

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        var entity = await _db.BlobContents.FindAsync(new object?[] { id }, ct);
        if (entity != null)
        {
            _db.BlobContents.Remove(entity);
            await _db.SaveChangesAsync(ct);
        }
    }
}

