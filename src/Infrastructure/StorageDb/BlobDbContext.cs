using Microsoft.EntityFrameworkCore;

namespace RekazDrive.Infrastructure.StorageDb;

public sealed class BlobDbContext : DbContext
{
    public BlobDbContext(DbContextOptions<BlobDbContext> options) : base(options) { }

    public DbSet<BlobMetadata> BlobMetadata => Set<BlobMetadata>();
    public DbSet<BlobContent> BlobContents => Set<BlobContent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BlobMetadata>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasMaxLength(512);
            b.Property(x => x.CreatedAtUtc).IsRequired();
            b.Property(x => x.Size).IsRequired();
            b.ToTable("blob_metadata");
        });

        modelBuilder.Entity<BlobContent>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasMaxLength(512);
            b.Property(x => x.Data).HasColumnType("BLOB");
            b.ToTable("blob_content");
        });
    }
}

public sealed class BlobMetadata
{
    public string Id { get; set; } = string.Empty;
    public long Size { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class BlobContent
{
    public string Id { get; set; } = string.Empty;
    public byte[]? Data { get; set; }
}

