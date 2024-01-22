using System.Collections.Generic;
using System.IO;

namespace Brio.Library;

public abstract class LibraryProviderBase
{
    public List<FileInfo> Files = new List<FileInfo>();

    public abstract void Scan();
}

public class LibraryFileProvider : LibraryProviderBase
{
    public readonly string DirectoryPath;

    public LibraryFileProvider(string directoryPath)
    {
        DirectoryPath = directoryPath;
    }

    public LibraryFileProvider(params string[] paths)
    {
        DirectoryPath = Path.Combine(paths);
    }

    public override void Scan()
    {
        if(!Directory.Exists(this.DirectoryPath))
            return;

        string[] filePaths = Directory.GetFiles(this.DirectoryPath, "*.*", SearchOption.AllDirectories);
        foreach(string file in filePaths)
        {
            FileInfo? info = FileInfo.GetInfo(file);
            if(info == null)
                continue;

            this.Files.Add(info);
        }
    }
}
