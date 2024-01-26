using Brio.Core;
using Brio.Library.Sources;
using Brio.Library.Tags;
using Brio.Resources;
using Dalamud.Interface.Internal;

namespace Brio.Files;

internal class MareCharacterDataFileInfo : FileTypeInfoBase<MareCharacterDataFile>
{
    public override string Name => "Mare Character Data";
    public override IDalamudTextureWrap Icon => ResourceProvider.Instance.GetResourceImage("Images.FileIcon_Mcdf.png");
    public override string Extension => ".mcdf";

    // No support for actually loading an mcdf, as that's handled by IPC-ing to Mare.
    // But this class is used for the library tags, so lets just fake it with an empty file. =)
    public override object? Load(string filePath) => new MareCharacterDataFile();
}

internal class MareCharacterDataFile : IFileMetadata
{
    public string? Description => null;
    public string? Author => null;
    public string? Version => null;
    public TagCollection? Tags => null;

    public void GetAutoTags(ref TagCollection tags)
    {
        tags.Add("Mare Synchronos");
    }
}
