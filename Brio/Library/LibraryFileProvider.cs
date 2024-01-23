using Brio.Resources;
using Dalamud.Interface.Internal;
using System.Collections.Generic;
using System.IO;

namespace Brio.Library;

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

        this.Scan(this.DirectoryPath, this);
    }

    public void Scan(string directory, ILibraryEntry parent)
    {
        if(!Directory.Exists(directory))
            return;

        string[] dirPaths = Directory.GetDirectories(directory);
        foreach(string dirPath in dirPaths)
        {
            LibraryDirectoryInfo dir = new(dirPath);
            parent.Add(dir);

            Scan(dirPath, dir);
        }

        string[] filePaths = Directory.GetFiles(directory, "*.*");
        foreach(string filePath in filePaths)
        {
            parent.Add(new LibraryFileInfo(filePath));
        }
    }
}

public class LibraryDirectoryInfo : ILibraryEntry
{
    public string Name { get; }
    public IEnumerable<ILibraryEntry>? Entries => _entries;
    public IDalamudTextureWrap? Icon => ResourceProvider.Instance.GetResourceImage("Images.FileIcon_Directory.png");

    private List<ILibraryEntry> _entries = new List<ILibraryEntry>();

    public LibraryDirectoryInfo(string path)
    {
        this.Name = System.IO.Path.GetFileNameWithoutExtension(path);

        if(this.Name.Length >= 60)
        {
            this.Name = this.Name.Substring(0, 55) + "...";
        }
    }

    public void Add(ILibraryEntry entry)
    {
        _entries.Add(entry);
    }
}

public class LibraryFileInfo : ILibraryEntry
{
    public readonly string FilePath;

    public LibraryFileInfo(string path)
    {
        this.FilePath = path;
        this.Name = System.IO.Path.GetFileNameWithoutExtension(path);

        if(this.Name.Length >= 60)
        {
            this.Name = this.Name.Substring(0, 55) + "...";
        }
    }

    public string Name { get; private set; }
    public IEnumerable<ILibraryEntry>? Entries => null;

    public IDalamudTextureWrap? Icon
    {
        get
        {
            // TODO: look up what type the file is, and get its icon from that
            string ext = System.IO.Path.GetExtension(this.FilePath);

            if(ext == ".pose")
            {
                return ResourceProvider.Instance.GetResourceImage("Images.FileIcon_Pose.png");
            }
            if(ext == ".chara")
            {
                return ResourceProvider.Instance.GetResourceImage("Images.FileIcon_Chara.png");
            }
            else
            {
                return ResourceProvider.Instance.GetResourceImage("Images.FileIcon_Unknown.png");
            }
        }
    }

    public void Add(ILibraryEntry entry)
    {
    }
}
