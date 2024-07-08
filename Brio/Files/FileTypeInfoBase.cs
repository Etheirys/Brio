using Brio.Library.Sources;
using Dalamud.Interface.Textures.TextureWraps;
using System;
using System.IO;

namespace Brio.Files;

internal abstract class FileTypeInfoBase
{
    public abstract string Name { get; }
    public abstract IDalamudTextureWrap Icon { get; }
    public abstract string Extension { get; }
    public abstract Type Type { get; }

    public abstract object? Load(string filePath);

    public bool IsFileType(Type type)
    {
        return type.IsAssignableFrom(Type);
    }

    public bool IsFileType<T>()
    {
        return typeof(T).IsAssignableFrom(Type);
    }

    public virtual bool IsFile(string path)
    {
        string ext = Path.GetExtension(path);
        return ext == Extension;
    }

    public virtual void DrawActions(FileEntry fileEntry, bool isModal)
    {
    }
}

internal abstract class FileTypeInfoBase<T> : FileTypeInfoBase
{
    public override Type Type => typeof(T);
}
