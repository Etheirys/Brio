using Brio.Resources;
using Dalamud.Interface.Internal;
using System.Collections.Generic;
using System.IO;

namespace Brio.Library;

public abstract class LibraryProviderBase : ILibraryEntry
{
    public List<FileInfo> Files = new List<FileInfo>();

    public string Name { get; protected set; } = string.Empty;
    public IEnumerable<ILibraryEntry>? Children => this.Files;
    public IDalamudTextureWrap? Icon { get; protected set; }

    public abstract void Scan();
}

public class LibraryFileProvider : LibraryProviderBase
{
    public readonly string DirectoryPath;

    public LibraryFileProvider(string name, string icon, string directoryPath)
    {
        this.Name = name;
        this.DirectoryPath = directoryPath;
        this.Icon = ResourceProvider.Instance.GetResourceImage(icon);
    }

    public LibraryFileProvider(string name, string icon, params string[] paths)
    {
        this.Name = name;
        this.DirectoryPath = Path.Combine(paths);
        this.Icon = ResourceProvider.Instance.GetResourceImage(icon);
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
