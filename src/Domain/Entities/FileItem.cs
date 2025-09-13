namespace RekazDrive.Domain.Entities;

public sealed class FileItem : DriveItem
{
    public long Size { get; set; }
    public string ContentType { get; set; } = "application/octet-stream"; 
    public string ContentKey { get; set; } = string.Empty;
}

