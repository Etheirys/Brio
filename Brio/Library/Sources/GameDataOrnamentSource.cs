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
            string rowName = $"Ornament {ornament.RowId}";
            var entry = new GameDataAppearanceEntry(this, ornament.Singular ?? rowName, ornament.Icon, ornament);
            entry.Tags.Add("Ornament").WithAlias("Fashion Accessory");
            entry.SourceInfo = rowName;
            Add(entry);
        }
    }
}
