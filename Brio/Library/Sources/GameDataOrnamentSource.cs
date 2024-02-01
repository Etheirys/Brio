using Brio.Resources;

namespace Brio.Library.Sources;

internal class GameDataOrnamentSource : GameDataAppearanceSourceBase
{
    public GameDataOrnamentSource(LibraryManager manager, GameDataProvider lumina)
        : base(manager, lumina)
    {
    }

    public override string Name => "Fashion Accessory";
    public override string Description => "Fashion Accessories from FFXIV";

    public override void Scan()
    {
        foreach(var (_, ornament) in Lumina.Ornaments)
        {
            string rowName = $"Ornament {ornament.RowId}";
            var entry = new GameDataAppearanceEntry(this, ornament.RowId, ornament.Singular ?? rowName, ornament.Icon, ornament);
            entry.Tags.Add("Ornament").WithAlias("Fashion Accessory");
            entry.SourceInfo = rowName;
            Add(entry);
        }
    }
}
