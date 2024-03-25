using System;
using System.Collections.Generic;

namespace Brio.Files;

internal class FileService
{
    private readonly IEnumerable<FileTypeInfoBase> _fileInfos;
    private readonly Dictionary<Type, FileTypeInfoBase> _typeInfoMap = new();

    public FileService(IEnumerable<FileTypeInfoBase> fileInfos)
    {
        _fileInfos = fileInfos;

        foreach (FileTypeInfoBase typeInfo in _fileInfos)
        {
            if (_typeInfoMap.ContainsKey(typeInfo.Type))
            {
                Brio.Log.Error($"Multiple file type info objects for file type: {typeInfo.Type}");
                continue;
            }

            _typeInfoMap.Add(typeInfo.Type, typeInfo);
        }
    }

    internal FileTypeInfoBase? GetFileTypeInfo(Type fileType)
    {
        FileTypeInfoBase? result = null;
        _typeInfoMap.TryGetValue(fileType, out result);
        return result;
    }

    internal FileTypeInfoBase? GetFileTypeInfo(string path)
    {
        foreach(FileTypeInfoBase fileType in _fileInfos)
        {
            if(fileType.IsFile(path))
            {
                return fileType;
            }
        }

        return null;
    }

    /// <summary>
    /// Attempt to load the file at the given path with any file type info that supports it.
    /// </summary>
    internal object? Load(string path)
    {
        Exception? lastException = null;

        foreach(FileTypeInfoBase typeInfo in _fileInfos)
        {
            if(typeInfo.IsFile(path))
            {
                try
                {
                    object? result = typeInfo.Load(path);

                    if(result != null)
                    {
                        return result;
                    }
                }
                catch(Exception ex)
                {
                    lastException = ex;
                }
            }
        }

        if(lastException != null)
            Brio.Log.Error(lastException, $"Error loading file: {path}");

        return null;
    }
}
