using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using RekazDrive.Infrastructure.StorageDb;

namespace RekazDrive.Infrastructure.StorageDb.Migrations;

[DbContext(typeof(BlobDbContext))]
public class BlobDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BlobMetadata>(b =>
        {
            b.Property<string>("Id").HasMaxLength(512);
            b.Property<long>("Size");
            b.Property<DateTimeOffset>("CreatedAtUtc");
            b.HasKey("Id");
            b.ToTable("blob_metadata");
        });

        modelBuilder.Entity<BlobContent>(b =>
        {
            b.Property<string>("Id").HasMaxLength(512);
            b.Property<byte[]>("Data");
            b.HasKey("Id");
            b.ToTable("blob_content");
        });
    }
}

