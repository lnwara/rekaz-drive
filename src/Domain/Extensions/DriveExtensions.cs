using System.Linq;
using RekazDrive.Domain.Entities;

namespace RekazDrive.Domain.Entities;

public static class DriveExtensions
{
    // TODO: This is not recursive; it does not traverse subfolders.
    public static long GetFolderSize(this Folder folder, IEnumerable<FileItem> allFiles)
    {
        if (folder == null) throw new ArgumentNullException(nameof(folder));
        if (allFiles == null) throw new ArgumentNullException(nameof(allFiles));
        return allFiles.Where(f => f.ParentId == folder.Id).Sum(f => f.Size);
    }
}

