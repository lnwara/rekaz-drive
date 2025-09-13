namespace RekazDrive.Application.Abstractions;

public interface IBlobStorage
{
    Task SaveAsync(string id, ReadOnlyMemory<byte> data, CancellationToken ct = default);
    Task<ReadOnlyMemory<byte>> GetAsync(string id, CancellationToken ct = default);
    Task<bool> ExistsAsync(string id, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
}

