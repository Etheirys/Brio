using Brio.Resources;

namespace Brio.Library.Sources;

internal class GameDataOrnamentSource : SourceBase
{
    private GameDataProvider _lumina;

    public GameDataOrnamentSource(GameDataProvider lumina)
        : base("Ornaments", ResourceProvider.Instance.GetResourceImage("Images.ProviderIcon_GameData.png"))
    {
        _lumina = lumina;
    }

    public override void Scan()
    {
        foreach(var (_, ornament) in _lumina.Ornaments)
        {
            var entry = new GameDataAppearanceEntry(this, ornament.Singular ?? $"Ornament {ornament.RowId}", ornament.Icon, ornament);
            entry.Tags.Add("Ornament").WithAlias("Fashion Accessory");
            Add(entry);
        }
    }
}
