using Brio.Config;
using Brio.Resources;
using Dalamud.Interface.Internal;
using System;
using System.Collections.Generic;
using System.IO;

namespace Brio.Library;

internal class LibraryManager : IDisposable, ILibraryEntry
{
    private readonly ConfigurationService _configurationService;

    public LibraryManager (ConfigurationService configurationService)
    {
        this._configurationService = configurationService;

        Categories.Add(new FavoritesCategory());
        Categories.Add(new PosesCategory());
        Categories.Add(new CharactersCategory());

        // TODO: add a configuration option to set the locations of these
        Providers.Add(new LibraryFileProvider("Brio Poses", "Images.ProviderIcon_Directory.png", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Brio", "Poses"));
        Providers.Add(new LibraryFileProvider("Brio Characters", "Images.ProviderIcon_Directory.png", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Brio", "Characters"));
        Providers.Add(new LibraryFileProvider("Anamnesis Poses", "Images.ProviderIcon_Anamnesis.png", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Anamnesis", "Poses"));
        Providers.Add(new LibraryFileProvider("Anamnesis Characters", "Images.ProviderIcon_Anamnesis.png", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Anamnesis", "Characters"));

        Scan();
    }

    public List<CategoryBase> Categories { get; init; } = new();
    public List<LibraryProviderBase> Providers { get; init; } = new();
    public List<FileInfo> AllFiles { get; init; } = new();
    public string Name => "Library";

    public IEnumerable<ILibraryEntry>? Children => this.Providers;
    public IDalamudTextureWrap? Icon => null;

    public void Dispose()
    {
    }

    private void Scan()
    {
        AllFiles.Clear();

        foreach (LibraryProviderBase provider in Providers)
        {
            provider.Scan();
            AllFiles.AddRange(provider.Files);
        }
    }
}

public class FileInfo : ILibraryEntry
{
    public readonly string FilePath;

    public FileInfo(string path)
    {
        this.FilePath = path;
        this.Name = System.IO.Path.GetFileNameWithoutExtension(path);

        if(this.Name.Length >= 60)
        {
            this.Name = this.Name.Substring(0, 55) + "...";
        }
    }

    public string Name { get; private set; }
    public IEnumerable<ILibraryEntry>? Children => null;

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

    public static FileInfo GetInfo(string filePath)
    {
        return new FileInfo(filePath);
    }
}

public interface ILibraryEntry
{
    public string Name { get; }
    public IEnumerable<ILibraryEntry>? Children { get; }
    public IDalamudTextureWrap? Icon { get; }
}
