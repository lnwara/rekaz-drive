namespace RekazDrive.Application.Abstractions;

public interface IBlobMetadataStore
{
    Task UpsertAsync(string id, long size, DateTimeOffset createdAtUtc, CancellationToken ct = default);
    Task<(string Id, long Size, DateTimeOffset CreatedAtUtc)?> GetAsync(string id, CancellationToken ct = default);
}

