using Brio.Files;
using Brio.Library.Tags;
using Brio.Resources;
using Brio.UI;
using Dalamud.Interface.Internal;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Brio.Library;

public class LibraryFileProvider : LibraryProviderBase
{
    public readonly string DirectoryPath;

    // We could probably scan the assembly for any types that have a FileTypeAttribute in them, but this is fine too.
    private static List<Type> fileTypes = new List<Type>()
    {
        typeof(AnamnesisCharaFile),
        typeof(CMToolPoseFile),
        typeof(PoseFile)
    };

    public LibraryFileProvider(string name, string icon, string directoryPath)
        : base(name, ResourceProvider.Instance.GetResourceImage(icon))
    {
        this.DirectoryPath = directoryPath;
    }

    public LibraryFileProvider(string name, string icon, params string[] paths)
          : base(name, ResourceProvider.Instance.GetResourceImage(icon))
    {
        this.DirectoryPath = Path.Combine(paths);
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
            FileTypeAttribute? fileTypeAttribute;
            Type? fileType;
            if(!GetFileType(filePath, out fileTypeAttribute, out fileType) || fileTypeAttribute == null || fileType == null)
                continue;

            parent.Add(new LibraryFileInfo(filePath, fileTypeAttribute, fileType));
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

public class LibraryDirectoryInfo : LibraryEntryBase
{
    private string _name;
    private IDalamudTextureWrap _icon;

    public LibraryDirectoryInfo(string path)
    {
        _name = System.IO.Path.GetFileNameWithoutExtension(path);
        if(_name.Length >= 60)
        {
            _name = this.Name.Substring(0, 55) + "...";
        }

        _icon = ResourceProvider.Instance.GetResourceImage("Images.FileIcon_Directory.png");
    }

    public override string Name => _name;
    public override IDalamudTextureWrap? Icon => _icon;
}

public class LibraryFileInfo : LibraryEntryBase
{
    public readonly string FilePath;
    public readonly FileTypeAttribute FileTypeAttribute;

    private string _name;
    private Type _fileType;
    private IDalamudTextureWrap? _icon;
    private bool _isFileIcon;

    public LibraryFileInfo(string path, FileTypeAttribute fileTypeAttribute, Type fileType)
    {
        this.FilePath = path;
        this.FileTypeAttribute = fileTypeAttribute;

        _name = System.IO.Path.GetFileNameWithoutExtension(path);
        if(_name.Length >= 60)
        {
            _name = _name.Substring(0, 55) + "...";
        }

        _fileType = fileType;

    
        // Get tags
        if(FileType != null && typeof(FileBase).IsAssignableFrom(FileType))
        {
            try
            {
                FileBase? doc = ResourceProvider.Instance.GetFileDocument(FilePath, FileType) as FileBase;
                if(doc != null)
                {
                    if (doc.Tags != null)
                        Tags.AddRange(doc.Tags);

                    TagCollection tags = Tags;
                    doc.GetAutoTags(ref tags);
                    Tags = tags;
                }
            }
            catch(Exception)
            {
            }
        }
    }

    public override string Name => _name;
    public override IDalamudTextureWrap? Icon => GetIcon();
    public override Type? FileType => _fileType;

    public override bool IsVisible
    {
        get => base.IsVisible;
        set
        {
            base.IsVisible = value;

            if(!value && _icon != null && _isFileIcon)
            {
                _icon.Dispose();
            }
        }
    }

    private IDalamudTextureWrap GetIcon()
    {
        if(_icon == null || _icon.ImGuiHandle == 0)
        {
            if(FileType != null && typeof(FileBase).IsAssignableFrom(FileType))
            {
                try
                {
                    FileBase? doc = ResourceProvider.Instance.GetFileDocument(FilePath, FileType) as FileBase;
                    if(doc != null && doc.Base64Image != null)
                    {
                        byte[] imgData = Convert.FromBase64String(doc.Base64Image);
                        _icon = UIManager.Instance.LoadImage(imgData);
                        _isFileIcon = true;
                        return _icon;
                    }
                }
                catch(Exception)
                {
                }
            }

            _icon = this.FileTypeAttribute.GetIcon(_fileType);
            _isFileIcon = false;
            return _icon;
        }

        return _icon;
    }

    public override void Dispose()
    {
        base.Dispose();

        if (_icon != null && _isFileIcon)
        {
            _icon.Dispose();
        }
    }
}
