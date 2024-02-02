using System;
using System.Collections.Generic;

namespace Brio.Files;

internal static class FileUtility
{
    internal static List<FileTypeInfoBase> FileTypeInfos = new();

    private static readonly Dictionary<Type, FileTypeInfoBase> _typeInfoMap = new();

    static FileUtility()
    {
        FileTypeInfos.Add(new AnamnesisCharaFileInfo());
        FileTypeInfos.Add(new CMToolPoseFileInfo());
        FileTypeInfos.Add(new PoseFileInfo());
        FileTypeInfos.Add(new MareCharacterDataFileInfo());

        foreach (FileTypeInfoBase typeInfo in FileTypeInfos)
        {
            if (_typeInfoMap.ContainsKey(typeInfo.Type))
            {
                Brio.Log.Error($"Multiple file type info objects for file type: {typeInfo.Type}");
                continue;
            }

            _typeInfoMap.Add(typeInfo.Type, typeInfo);
        }
    }

    internal static FileTypeInfoBase? GetFileTypeInfo(Type fileType)
    {
        FileTypeInfoBase? result = null;
        _typeInfoMap.TryGetValue(fileType, out result);
        return result;
    }

    /// <summary>
    /// Attempt to load the file at the given path with any file type info that supports it.
    /// </summary>
    internal static object? Load(string path)
    {
        Exception? lastException = null;

        foreach(FileTypeInfoBase typeInfo in FileTypeInfos)
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
