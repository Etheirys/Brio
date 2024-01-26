using Brio.Resources;
using Dalamud.Interface.Internal;

namespace Brio.Library.Sources;

internal class GameDataCompanionSource : SourceBase
{
    private GameDataProvider _lumina;

    public GameDataCompanionSource(GameDataProvider lumina)
        : base()
    {
        _lumina = lumina;
    }

    public override string Name => "Minions";
    public override IDalamudTextureWrap? Icon => ResourceProvider.Instance.GetResourceImage("Images.ProviderIcon_GameData.png");
    public override string Description => "Minions from FFXIV";

    public override void Scan()
    {
        foreach(var (_, companion) in _lumina.Companions)
        {
            string rowName = $"Companion {companion.RowId}";
            var entry = new GameDataAppearanceEntry(this, companion.Singular ?? rowName, companion.Icon, companion);
            entry.Tags.Add("Companion").WithAlias("Minion");
            entry.SourceInfo = rowName;
            Add(entry);
        }
    }
}
