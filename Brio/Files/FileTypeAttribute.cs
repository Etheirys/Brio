using Brio.Resources;
using Dalamud.Interface.Internal;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Brio.Files;
public class FileTypeAttribute : Attribute
{
    public readonly string Name;
    public readonly string ResourceIcon;
    public readonly string Extension;
    public readonly string LoadMethod;

    private static readonly Dictionary<Type, LoadDelegate> _loadDelegates = new();

    public FileTypeAttribute(string name, string resourceIcon, string extension, string loadMethod)
    {
        Name = name;
        ResourceIcon = resourceIcon;
        Extension = extension;
        LoadMethod = loadMethod;
    }

    public bool IsFile(string path)
    {
        return System.IO.Path.GetExtension(path) == Extension;
    }

    public IDalamudTextureWrap GetIcon(Type type)
    {
        return ResourceProvider.Instance.GetResourceImage(this.ResourceIcon);
    }

    public delegate object? LoadDelegate(string filePath);

    public static LoadDelegate? GetLoadMethod(Type targetType)
    {
        LoadDelegate? dlg = null;

        lock(_loadDelegates)
        {
            if(_loadDelegates.TryGetValue(targetType, out dlg) && dlg != null)
            {
                return dlg;
            }
        }

        FileTypeAttribute? attribute = targetType.GetCustomAttribute<FileTypeAttribute>();
        if(attribute == null)
            return null;

        MethodInfo? loadMethod = targetType.GetMethod(attribute.LoadMethod);
        if(loadMethod == null)
            return null;

        try
        {
            dlg = loadMethod.CreateDelegate<LoadDelegate>();
        }
        catch(Exception ex)
        {
            Brio.Log.Error(ex, $"Error creating load delegate for file type {targetType}");
            return null;
        }

        if(dlg != null)
        {
            lock(_loadDelegates)
            {
                _loadDelegates.Add(targetType, dlg);
            }
        }

        return dlg;
    }
}
