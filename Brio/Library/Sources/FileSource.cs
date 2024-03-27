using Brio.Config;
using Brio.Files;
using Brio.Library.Tags;
using Brio.Resources;
using Brio.UI;
using Brio.UI.Controls.Stateless;
using Dalamud.Interface;
using Dalamud.Interface.Internal;
using ImGuiNET;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Brio.Library.Sources;

internal class FileSource : SourceBase
{
    public readonly string DirectoryPath = string.Empty;

    private string _name;
    private FileService _fileService;

    public FileSource(FileService fileService, LibraryConfiguration.FileSourceConfig config)
        : base()
    {
        _fileService = fileService;
        _name = config.Name;

        if(config.Root != null)
        {
            DirectoryPath = Environment.GetFolderPath((Environment.SpecialFolder)config.Root) + config.Path;
        }
        else if(config.Path != null)
        {
            DirectoryPath = config.Path;
        }
    }

    public FileSource(FileService fileService, string name, string directoryPath)
        : base()
    {
        _fileService = fileService;
        _name = name;
        DirectoryPath = directoryPath;
    }

    public FileSource(FileService fileService, string name, params string[] paths)
          : base()
    {
        _fileService = fileService;
        _name = name;
        DirectoryPath = Path.Combine(paths);
    }

    public override string Name => _name;
    public override IDalamudTextureWrap? Icon => ResourceProvider.Instance.GetResourceImage("Images.ProviderIcon_Directory.png");
    public override string Description => DirectoryPath;

    protected override string GetInternalId()
    {
        // All file sources share the same internal Id, as the files themselves are unique on the
        // file system.
        return $"File";
    }

    public override void Scan()
    {
        if(!Directory.Exists(DirectoryPath))
            return;

        Scan(DirectoryPath, this);
    }

    public void Scan(string directory, GroupEntryBase parent)
    {
        if(!Directory.Exists(directory))
            return;

        string[] dirPaths = Directory.GetDirectories(directory);
        foreach(string dirPath in dirPaths)
        {
            DirectoryEntry dir = new(this, dirPath);
            parent.Add(dir);

            Scan(dirPath, dir);
        }

        string[] filePaths = Directory.GetFiles(directory, "*.*");
        foreach(string filePath in filePaths)
        {
            FileTypeInfoBase? fileTypeInfo = _fileService.GetFileTypeInfo(filePath);
            if(fileTypeInfo == null)
                continue;

            parent.Add(new FileEntry(this, filePath, fileTypeInfo));
        }
    }
}

public interface IFileMetadata
{
    string? Author { get; }
    string? Description { get; }
    string? Version { get; }
    TagCollection? Tags { get; }

    void GetAutoTags(ref TagCollection tags);
}

internal class DirectoryEntry : GroupEntryBase
{
    private string _name;
    private IDalamudTextureWrap _icon;
    private string _path;

    public DirectoryEntry(FileSource source, string path)
        : base(source)
    {
        _path = path;
        _name = Path.GetFileNameWithoutExtension(path);
        if(_name.Length >= 60)
        {
            _name = Name.Substring(0, 55) + "...";
        }

        _icon = ResourceProvider.Instance.GetResourceImage("Images.FileIcon_Directory.png");
    }

    public override string Name => _name;
    public override IDalamudTextureWrap? Icon => _icon;

    protected override string GetInternalId()
    {
        return _path;
    }

    public override void DrawActions(bool isModal)
    {
        base.DrawActions(isModal);

        if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            if(ImBrio.FontIconButton(FontAwesomeIcon.FolderOpen))
            {
                Process.Start("explorer.exe", this._path);
            }

            if(ImGui.IsItemHovered())
                ImGui.SetTooltip("Open folder");

            ImGui.SameLine();
        }
    }
}

internal class FileEntry : ItemEntryBase
{
    public readonly string FilePath;

    private string _name;
    private FileTypeInfoBase _fileInfo;
    private IDalamudTextureWrap? _previewImage;
    private string? _description;
    private string? _author;
    private string? _version;

    public FileEntry(FileSource source, string path, FileTypeInfoBase fileInfo)
        : base(source)
    {
        _fileInfo = fileInfo;

        this.FilePath = path;
        this.SourceInfo = Path.GetRelativePath(source.DirectoryPath, path);

        _name = Path.GetFileNameWithoutExtension(path);
        if(_name.Length >= 60)
        {
            _name = _name.Substring(0, 55) + "...";
        }

        try
        {
            if(_fileInfo.IsFileType<IFileMetadata>() == true)
            {
                IFileMetadata? file = _fileInfo.Load(path) as IFileMetadata;
                if(file != null)
                {
                    if(file.Tags != null)
                        Tags.AddRange(file.Tags);

                    TagCollection tags = Tags;
                    file.GetAutoTags(ref tags);
                    Tags = tags;

                    _description = file.Description;
                    _author = file.Author;
                    _version = file.Version;
                }
            }
        }
        catch(Exception)
        {
        }
    }

    public override string Name => _name;
    public override string? Description => _description;
    public override string? Author => _author;
    public override string? Version => _version;
    public override IDalamudTextureWrap? Icon => GetIcon();
    public override IDalamudTextureWrap? PreviewImage => GetPreviewImage();
    public override Type LoadsType => _fileInfo.Type;
    public FileTypeInfoBase FileTypeInfo => _fileInfo;

    public override bool IsVisible
    {
        get => base.IsVisible;
        set
        {
            base.IsVisible = value;

            if(!value)
            {
                _previewImage?.Dispose();
            }
        }
    }

    private IDalamudTextureWrap GetIcon()
    {
        IDalamudTextureWrap? preview = GetPreviewImage();
        if(preview != null)
            return preview;

        if(_fileInfo == null)
            return ResourceProvider.Instance.GetResourceImage("Images.FileIcon_Unknown.png");

        return _fileInfo.Icon;
    }

    private IDalamudTextureWrap? GetPreviewImage()
    {
        if(_previewImage == null || _previewImage.ImGuiHandle == 0)
        {
            try
            {
                if(_fileInfo?.IsFileType<JsonDocumentBase>() == true)
                {
                    JsonDocumentBase? file = _fileInfo.Load(FilePath) as JsonDocumentBase;
                    if(file != null && file.Base64Image != null)
                    {
                        byte[] imgData = Convert.FromBase64String(file.Base64Image);
                        _previewImage = UIManager.Instance.LoadImage(imgData);
                        return _previewImage;
                    }
                }
            }
            catch(Exception)
            {
            }
        }

        return _previewImage;
    }

    public override void Dispose()
    {
        base.Dispose();
        _previewImage?.Dispose();
    }

    protected override string GetInternalId()
    {
        return FilePath;
    }

    public override object? Load()
    {
        if(FileTypeInfo == null)
            return null;

        return FileTypeInfo.Load(this.FilePath);
    }

    public override void DrawActions(bool isModal)
    {
        base.DrawActions(isModal);

        if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            string? dirPath = Path.GetDirectoryName(this.FilePath);
            if(dirPath != null)
            {
                if(ImBrio.FontIconButton(FontAwesomeIcon.FolderOpen))
                {
                    Process.Start("explorer.exe", dirPath);
                }

                if(ImGui.IsItemHovered())
                    ImGui.SetTooltip("Open containing folder");

                ImGui.SameLine();
            }
        }

        if(FileTypeInfo != null)
        {
            FileTypeInfo.DrawActions(this, isModal);
        }
    }
}
