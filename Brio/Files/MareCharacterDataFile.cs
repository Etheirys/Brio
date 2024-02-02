using Brio.Capabilities.Actor;
using Brio.Entities.Actor;

using Brio.Library.Sources;
using Brio.Library.Tags;
using Brio.Resources;
using Dalamud.Interface.Internal;
using System.Threading.Tasks;

namespace Brio.Files;

internal class MareCharacterDataFileInfo : FileTypeInfoBase<MareCharacterDataFile>
{
    public override string Name => "Mare Character Data";
    public override IDalamudTextureWrap Icon => ResourceProvider.Instance.GetResourceImage("Images.FileIcon_Mcdf.png");
    public override string Extension => ".mcdf";

    // No support for actually loading an mcdf, as that's handled by IPC-ing to Mare.
    // But this class is used for the library tags, so lets just fake it with an empty file. =)
    public override object? Load(string filePath) => new MareCharacterDataFile(filePath);


    private Task Apply(FileEntry entry, ActorEntity actor)
    {
        ActorAppearanceCapability? capability;
        if(actor.TryGetCapability<ActorAppearanceCapability>(out capability) && capability != null)
        {
            return capability.LoadMcdfAsync(entry.FilePath);
        }

        return Task.CompletedTask;
    }
}

internal class MareCharacterDataFile : IFileMetadata
{
    private string _filePath;

    public string? Description => null;
    public string? Author => null;
    public string? Version => null;
    public TagCollection? Tags => null;

    public MareCharacterDataFile(string  filePath)
    {
        _filePath = filePath;
    }

    public string GetPath()
    {
        return _filePath;
    }

    public void GetAutoTags(ref TagCollection tags)
    {
        tags.Add("Mare Synchronos");
    }
}
