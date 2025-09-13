using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RekazDrive.Application.Abstractions;
using RekazDrive.Application.Services;
using Xunit;

namespace RekazDrive.Application.Tests;

public class BlobServiceTests
{
    private sealed class FakeBlobStorage : IBlobStorage
    {
        private readonly Dictionary<string, byte[]> _store = new();

        public Task SaveAsync(string id, ReadOnlyMemory<byte> data, CancellationToken ct = default)
        { _store[id] = data.ToArray(); return Task.CompletedTask; }

        public Task<ReadOnlyMemory<byte>> GetAsync(string id, CancellationToken ct = default)
        {
            if (!_store.TryGetValue(id, out var bytes))
                throw new System.IO.FileNotFoundException("Blob not found", id);
            return Task.FromResult<ReadOnlyMemory<byte>>(bytes);
        }

        public Task<bool> ExistsAsync(string id, CancellationToken ct = default)
            => Task.FromResult(_store.ContainsKey(id));

        public Task DeleteAsync(string id, CancellationToken ct = default)
        { _store.Remove(id); return Task.CompletedTask; }
    }

    private sealed class FakeMetadataStore : IBlobMetadataStore
    {
        private readonly Dictionary<string, (long Size, DateTimeOffset CreatedAtUtc)> _meta = new();

        public Task UpsertAsync(string id, long size, DateTimeOffset createdAtUtc, CancellationToken ct = default)
        { _meta[id] = _meta.TryGetValue(id, out var existing) ? (size, existing.CreatedAtUtc) : (size, createdAtUtc); return Task.CompletedTask; }

        public Task<(string Id, long Size, DateTimeOffset CreatedAtUtc)?> GetAsync(string id, CancellationToken ct = default)
        {
            return Task.FromResult(_meta.TryGetValue(id, out var v)
                ? (ValueTuple<string, long, DateTimeOffset>?) (id, v.Size, v.CreatedAtUtc)
                : null);
        }
    }

    [Fact]
    public async Task Store_ValidBase64_SavesBytesAndMetadata()
    {
        var svc = new BlobService(new FakeBlobStorage(), new FakeMetadataStore());
        var b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("Hello"));

        var stored = await svc.StoreAsync(b64, CancellationToken.None);

        var res = await svc.RetrieveAsync(stored.Id, CancellationToken.None);
        Assert.Equal(stored.Id, res.Id);
        Assert.Equal(5, res.Size);
        Assert.Equal("Hello", Encoding.UTF8.GetString(Convert.FromBase64String(res.Data)));
    }

    [Fact]
    public async Task Store_InvalidBase64_IsRejected()
    {
        var svc = new BlobService(new FakeBlobStorage(), new FakeMetadataStore());
        await Assert.ThrowsAsync<FormatException>(() => svc.StoreAsync("not-base64", CancellationToken.None));
    }

    [Fact]
    public async Task Retrieve_ReturnsExpectedShape()
    {
        var storage = new FakeBlobStorage();
        var meta = new FakeMetadataStore();
        var svc = new BlobService(storage, meta);

        var id = "a/b";
        var data = Encoding.UTF8.GetBytes("ABC");
        await storage.SaveAsync(id, data, CancellationToken.None);
        var created = DateTimeOffset.UtcNow;
        await meta.UpsertAsync(id, data.Length, created, CancellationToken.None);

        var res = await svc.RetrieveAsync(id, CancellationToken.None);
        Assert.Equal(id, res.Id);
        Assert.Equal(Convert.ToBase64String(data), res.Data);
        Assert.Equal(3, res.Size);
        Assert.Equal(created, res.CreatedAt);
    }
}
