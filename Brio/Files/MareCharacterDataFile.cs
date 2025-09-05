using Brio.Capabilities.Actor;
using Brio.Config;
using Brio.Entities;
using Brio.Entities.Actor;
using Brio.Library.Sources;
using Brio.Library.Tags;
using Brio.Resources;
using Dalamud.Interface.Textures.TextureWraps;

namespace Brio.Files;

public class MareCharacterDataFileInfo : AppliableActorFileInfoBase<MareCharacterDataFile>
{
    public MareCharacterDataFileInfo(EntityManager entityManager, ConfigurationService configurationService)
    : base(entityManager, configurationService)
    {
    }

    public override string Name => "MCDF";
    public override IDalamudTextureWrap Icon => ResourceProvider.Instance.GetResourceImage("Images.FileIcon_Mcdf.png");
    public override string Extension => ".mcdf";

    // Mare died, Brio can now load MCDF's
    public override object? Load(string filePath) => new MareCharacterDataFile(filePath);

    protected override void Apply(MareCharacterDataFile file, ActorEntity actor, bool asExpression = false)
    {
        ActorAppearanceCapability? capability;
        if(actor.TryGetCapability<ActorAppearanceCapability>(out capability) && capability != null)
        {
            _ = capability.LoadMCDF(file.GetPath());
        }
    }
}

public class MareCharacterDataFile : IFileMetadata
{
    private string _filePath;

    public string? Description => null;
    public string? Author => null;
    public string? Version => null;
    public TagCollection? Tags => null;

    public MareCharacterDataFile(string filePath)
    {
        _filePath = filePath;
    }

    public string GetPath()
    {
        return _filePath;
    }

    public void GetAutoTags(ref TagCollection tags)
    {
        tags.Add("MCDF");
    }
}
