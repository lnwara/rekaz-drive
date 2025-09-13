using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace RekazDrive.Infrastructure.StorageDb.Migrations;

public partial class InitialBlobSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "blob_metadata",
            columns: table => new
            {
                Id = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                Size = table.Column<long>(type: "INTEGER", nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_blob_metadata", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "blob_content",
            columns: table => new
            {
                Id = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                Data = table.Column<byte[]>(type: "BLOB", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_blob_content", x => x.Id);
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "blob_content");
        migrationBuilder.DropTable(name: "blob_metadata");
    }
}

