using Brio.Resources;
using Dalamud.Interface.Internal;

namespace Brio.Library.Sources;

internal class GameDataOrnamentSource : SourceBase
{
    private GameDataProvider _lumina;

    public GameDataOrnamentSource(GameDataProvider lumina)
        : base()
    {
        _lumina = lumina;
    }


    public override string Name => "Fashion Accessory";
    public override IDalamudTextureWrap? Icon => ResourceProvider.Instance.GetResourceImage("Images.ProviderIcon_GameData.png");
    public override string Description => "Fashion Accessories from FFXIV";

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
