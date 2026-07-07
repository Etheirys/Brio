using System;

namespace Brio.Game.Core;

public struct ObjectPath
{
    public string Path { get; private set; }
    public bool IsValid { get; private set; }
    public ObjectPathKind PathKind { get; private set; }

    public ObjectPath(string path)
    {
        if(string.IsNullOrWhiteSpace(path))
        {
            Path = string.Empty;
            IsValid = false;
            PathKind = ObjectPathKind.Invalid;
            return;
        }

        Path = path;
        IsValid = Brio.GameFileExists(path);
        PathKind = GetPathKind();
    }

    public readonly ObjectPathKind GetPathKind()
    {
        if(!IsValid)
            return ObjectPathKind.Invalid;

        if(HasExtension(Path, ".mdl"))
            return ObjectPathKind.Model;
        if(HasExtension(Path, ".sgb"))
            return ObjectPathKind.SharedGroup;
        if(HasExtension(Path, ".avfx"))
            return ObjectPathKind.VFX;
        if(HasExtension(Path, ".lgb"))
            return ObjectPathKind.Level;
        if(HasExtension(Path, ".pap"))
            return ObjectPathKind.Pap;
        if(HasExtension(Path, ".sklb"))
            return ObjectPathKind.Sklb;
        if(HasExtension(Path, ".atex"))
            return ObjectPathKind.Atex;
        if(HasExtension(Path, ".tex"))
            return ObjectPathKind.Texture;
        if(HasExtension(Path, ".scd"))
            return ObjectPathKind.Sound;
        if(HasExtension(Path, ".tmb"))
            return ObjectPathKind.Timeline;

        return ObjectPathKind.Unknown;
    }

    public readonly bool HasExtension(string path, string extension)
        => IsValid && path.EndsWith(extension, StringComparison.OrdinalIgnoreCase);
}

public enum ObjectPathKind
{
    Invalid,
    Unknown,
    Model,
    SharedGroup,
    Timeline,
    Sklb,
    Level,
    Pap,
    Texture,
    VFX,
    Atex,
    Sound
}
