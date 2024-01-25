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

public class FileSource : SourceBase
{
    public readonly string DirectoryPath;

    // We could probably scan the assembly for any types that have a FileTypeAttribute in them, but this is fine too.
    private static List<Type> fileTypes = new List<Type>()
    {
        typeof(AnamnesisCharaFile),
        typeof(CMToolPoseFile),
        typeof(PoseFile),
        typeof(MareCharacterDataFile),
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

    public void Scan(string directory, ILibraryEntry parent)
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
            FileTypeAttribute? fileTypeAttribute;
            Type? fileType;
            if(!GetFileType(filePath, out fileTypeAttribute, out fileType) || fileTypeAttribute == null || fileType == null)
                continue;

            parent.Add(new FileEntry(this, filePath, fileTypeAttribute, fileType));
        }
    }

    private bool GetFileType(string path, out FileTypeAttribute? file, out Type? type)
    {
        file = null;
        type = null;

        foreach(Type possibleType in fileTypes)
        {
            FileTypeAttribute? fileType = possibleType.GetCustomAttribute<FileTypeAttribute>();
            if(fileType == null)
                continue;

            if(fileType.IsFile(path))
            {
                file = fileType;
                type = possibleType;
                return true;
            }
        }

        return false;
    }
}

public class DirectoryEntry : LibraryEntryBase
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

public interface IFile
{
    string? Author { get; }
    string? Description { get; }
    string? Version { get; }
    TagCollection? Tags { get; }

    void GetAutoTags(ref TagCollection tags);
}

public class FileEntry : LibraryEntryBase
{
    public readonly string FilePath;
    public readonly FileTypeAttribute FileTypeAttribute;

    private string _name;
    private Type _fileType;
    private IDalamudTextureWrap? _previewImage;
    private FileTypeAttribute.LoadDelegate? _loadDelegate;

    public FileEntry(FileSource source, string path, FileTypeAttribute fileTypeAttribute, Type fileType)
        : base(source)
    {
        FilePath = path;
        FileTypeAttribute = fileTypeAttribute;

        SourceInfo = Path.GetRelativePath(source.DirectoryPath, path);

        _name = Path.GetFileNameWithoutExtension(path);
        if(_name.Length >= 60)
        {
            _name = _name.Substring(0, 55) + "...";
        }

        _fileType = fileType;

        // if this type has a load method, invoke it to load the file
        try
        {
            if(FileType != null)
            {
                _loadDelegate = FileTypeAttribute.GetLoadMethod(FileType);
            }

           
            if(_loadDelegate != null)
            {
                object? file = _loadDelegate.Invoke(path);
                if(file is IFile fileInterface)
                {
                    if(fileInterface.Tags != null)
                        Tags.AddRange(fileInterface.Tags);

                    TagCollection tags = Tags;
                    fileInterface.GetAutoTags(ref tags);
                    Tags = tags;

                    Description = fileInterface.Description;
                    Author = fileInterface.Author;
                    Version = fileInterface.Version;
                }
            }
        }
        catch (Exception)
        {
        }
    }

    public override string Name => _name;
    public override IDalamudTextureWrap? Icon => GetIcon();
    public override IDalamudTextureWrap? PreviewImage => GetPreviewImage();
    public override Type? FileType => _fileType;

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

        return FileTypeAttribute.GetIcon(_fileType);
    }

    private IDalamudTextureWrap? GetPreviewImage()
    {
        if(_previewImage == null || _previewImage.ImGuiHandle == 0)
        {
            if(FileType != null && typeof(JsonDocumentBase).IsAssignableFrom(FileType))
            {
                try
                {
                    object? file = _loadDelegate?.Invoke(FilePath);

                    if(file != null && file is JsonDocumentBase doc && doc.Base64Image != null)
                    {
                        byte[] imgData = Convert.FromBase64String(doc.Base64Image);
                        _previewImage = UIManager.Instance.LoadImage(imgData);
                        return _previewImage;
                    }
                }
                catch(Exception)
                {
                }
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
