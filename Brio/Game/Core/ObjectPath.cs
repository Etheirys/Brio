using Brio.Resources.Extra;
using FFXIVClientStructs.FFXIV.Client.Game;
using System;
using System.IO.Hashing;
using System.Text;

namespace Brio.Game.Core;

public struct ObjectPath
{
    private readonly string[] PaidExpansions = ["Dawntrail", "Endwalker"];

    public string Path { get; private set; }
    public bool IsValid { get; private set; }
    public ObjectPathKind PathKind { get; private set; }
    public ulong Hash { get; private set; }

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
        Hash = HashPath(path);

        unsafe
        {
            // TODO, this doesn't catch every path I think? Should also do this on other code paths like when trying to spawn anything from the catalog.
            // Should also at some point look into actually mergeing this with the other path thing in PathDatabase
            if(Conditions.Instance()->OnFreeTrial && PaidExpansions.Contains(PathIndex.ParsePath(path).Expansion))
            {
                IsValid = false;
                PathKind = ObjectPathKind.NotAvailable;
                Brio.NotifyInfo("This object is not available on the free trial version of the game.");
            }
            else
            {
                IsValid = Brio.GameFileExists(path);
            }
        }

        PathKind = GetPathKind();
    }

    public static ulong HashPath(string path)
    {
        var maxBytes = Encoding.UTF8.GetMaxByteCount(path.Length);

        Span<byte> buffer = new byte[maxBytes];
        int written = Encoding.UTF8.GetBytes(path, buffer);
        return XxHash3.HashToUInt64(buffer[..written]);
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
    NotAvailable,
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
