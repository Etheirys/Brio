using Brio.Config;
using System;
using System.Collections.Generic;

namespace Brio.Library;

internal class LibraryManager : IDisposable
{
    private readonly ConfigurationService _configurationService;

    public LibraryManager (ConfigurationService configurationService)
    {
        this._configurationService = configurationService;

        Categories.Add(new FavoritesCategory());
        Categories.Add(new PosesCategory());
        Categories.Add(new CharactersCategory());

        // TODO: add a configuration option to set the locations of these
        Providers.Add(new LibraryFileProvider(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Brio", "Poses"));
        Providers.Add(new LibraryFileProvider(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Brio", "Characters"));
        Providers.Add(new LibraryFileProvider(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Anamnesis", "Poses"));
        Providers.Add(new LibraryFileProvider(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Anamnesis", "Characters"));

        Scan();
    }

    public List<CategoryBase> Categories { get; init; } = new();
    public List<LibraryProviderBase> Providers { get; init; } = new();
    public List<FileInfo> AllFiles { get; init; } = new();

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

public class FileInfo
{
    public readonly string Path;
    public readonly string Name;
    public readonly string Icon;

    public FileInfo(string path)
    {
        this.Path = path;
        this.Name = System.IO.Path.GetFileNameWithoutExtension(path);

        if(this.Name.Length >= 60)
        {
            this.Name = this.Name.Substring(0, 55) + "...";
        }

        // TODO: look up what type the file is, and get its icon from that
        string ext = System.IO.Path.GetExtension(path);
        this.Icon = "Images.FileIcon_Unknown.png";
        if (ext == ".pose")
            this.Icon = "Images.FileIcon_Pose.png";
        if(ext == ".chara")
            this.Icon = "Images.FileIcon_Chara.png";
    }

    public static FileInfo GetInfo(string filePath)
    {
        return new FileInfo(filePath);
    }
}
