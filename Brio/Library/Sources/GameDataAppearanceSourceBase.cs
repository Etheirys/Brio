using Brio.Capabilities.Actor;
using Brio.Entities.Actor;
using Brio.Game.Actor.Appearance;
using Brio.Resources;
using Dalamud.Interface.Internal;
using System.Threading.Tasks;

namespace Brio.Library.Sources;

internal abstract class GameDataAppearanceSourceBase : SourceBase
{
    private GameDataProvider _lumina;

    public GameDataAppearanceSourceBase(LibraryManager manager, GameDataProvider lumina)
     : base()
    {
        _lumina = lumina;
    }

    public override IDalamudTextureWrap? Icon => ResourceProvider.Instance.GetResourceImage("Images.ProviderIcon_GameData.png");
    protected GameDataProvider Lumina => _lumina;

    private static async Task Apply(GameDataAppearanceEntry entry, ActorEntity actor)
    {
        ActorAppearanceCapability? capability;
        if(actor.TryGetCapability<ActorAppearanceCapability>(out capability) && capability != null)
        {
            await capability.SetAppearance(entry.Appearance, AppearanceImportOptions.All);
        }
    }

}
