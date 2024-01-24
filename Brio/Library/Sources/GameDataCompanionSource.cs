using Brio.Resources;

namespace Brio.Library.Sources;

internal class GameDataCompanionSource : SourceBase
{
    private GameDataProvider _lumina;

    public GameDataCompanionSource(GameDataProvider lumina)
        : base("Companions", ResourceProvider.Instance.GetResourceImage("Images.ProviderIcon_GameData.png"))
    {
        _lumina = lumina;
    }

    public override void Scan()
    {
        foreach(var (_, companion) in _lumina.Companions)
        {
            var entry = new GameDataAppearanceEntry(this, companion.Singular ?? $"Companion {companion.RowId}", companion.Icon, companion);
            entry.Tags.Add("Companion").WithAlias("Minion");
            Add(entry);
        }
    }
}
