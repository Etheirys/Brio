using Brio.Files;
using Brio.Library.Tags;
using Brio.Resources;
using Brio.UI;
using Dalamud.Interface.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Brio.Library.Sources;

internal class FileSource : SourceBase
{
    public readonly string DirectoryPath;

    // We could probably put these in a file type manager or something,
    // but currently only the library needs them, so here is fine too.
    private static List<FileInfoBase> fileTypes = new()
    {
        new AnamnesisCharaFileInfo(),
        new CMToolPoseFileInfo(),
        new PoseFileInfo(),
        new MareCharacterDataFileInfo(),
    };

    public FileSource(string name, string icon, string directoryPath)
        : base(name, ResourceProvider.Instance.GetResourceImage(icon))
    {
        DirectoryPath = directoryPath;
    }

    public FileSource(string name, string icon, params string[] paths)
          : base(name, ResourceProvider.Instance.GetResourceImage(icon))
    {
        DirectoryPath = Path.Combine(paths);
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
            FileInfoBase? fileTypeInfo = GetFileType(filePath);
            if(fileTypeInfo == null)
                continue;

            parent.Add(new FileEntry(this, filePath, fileTypeInfo));
        }
    }

    private FileInfoBase? GetFileType(string path)
    {
        foreach (FileInfoBase fileType in fileTypes)
        {
            if(fileType.IsFile(path))
            {
                return fileType;
            }
        }

        return null;
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

    public DirectoryEntry(FileSource source, string path)
        : base(source)
    {
        _name = Path.GetFileNameWithoutExtension(path);
        if(_name.Length >= 60)
        {
            _name = Name.Substring(0, 55) + "...";
        }

        _icon = ResourceProvider.Instance.GetResourceImage("Images.FileIcon_Directory.png");
    }

    public override string Name => _name;
    public override IDalamudTextureWrap? Icon => _icon;
}

internal class FileEntry : ItemEntryBase
{
    public readonly string FilePath;

    private string _name;
    private FileInfoBase _fileInfo;
    private IDalamudTextureWrap? _previewImage;
    private string? _description;
    private string? _author;
    private string? _version;

    public FileEntry(FileSource source, string path, FileInfoBase fileInfo)
        : base(source)
    {
        _fileInfo = fileInfo;

        FilePath = path;
        SourceInfo = Path.GetRelativePath(source.DirectoryPath, path);

        _name = Path.GetFileNameWithoutExtension(path);
        if(_name.Length >= 60)
        {
            _name = _name.Substring(0, 55) + "...";
        }

        // if this type has a load method, invoke it to load the file
        try
        {
            if (_fileInfo.IsFileType<IFileMetadata>() == true)
            {
                IFileMetadata? file = _fileInfo.Load(path) as IFileMetadata;
                if (file != null)
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
        catch (Exception)
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

        if (_fileInfo == null)
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
}
