using Brio.Capabilities.Actor;
using Brio.Entities.Actor;
using Brio.Game.Actor.Appearance;
using Brio.Library.Actions;
using Brio.Resources;
using Dalamud.Interface.Internal;
using System.Threading.Tasks;

namespace Brio.Library.Sources;

internal abstract class GameDataAppearanceSourceBase : SourceBase
{
    private GameDataProvider _lumina;
    private static EntryActionBase applyAction = new ApplyToSelectedActorAction<GameDataAppearanceEntry>(Apply, true);


    public GameDataAppearanceSourceBase(LibraryManager manager, GameDataProvider lumina)
     : base()
    {
        _lumina = lumina;
        manager.RegisterAction(applyAction);
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
