using Brio.Resources;
using Dalamud.Interface.Internal;
using System;

namespace Brio.Files;
public class FileTypeAttribute : Attribute
{
    public readonly string Name;
    public readonly string ResourceIcon;
    public readonly string Extension;

    public FileTypeAttribute(string name, string resourceIcon, string extension)
    {
        Name = name;
        ResourceIcon = resourceIcon;
        Extension = extension;
    }

    public bool IsFile(string path)
    {
        return System.IO.Path.GetExtension(path) == Extension;
    }

    public IDalamudTextureWrap GetIcon(Type type)
    {
        return ResourceProvider.Instance.GetResourceImage(this.ResourceIcon);
    }
}
