using Brio.Entities;
using Brio.Resources;

namespace Brio.Library.Sources;

internal class GameDataOrnamentSource : GameDataAppearanceSourceBase
{
    public GameDataOrnamentSource(GameDataProvider lumina, EntityManager entityManager)
        : base(lumina, entityManager)
    {
    }

    public override string Name => "Fashion Accessory";
    public override string Description => "Fashion Accessories from FFXIV";

    public override void Scan()
    {
        foreach(var (_, ornament) in Lumina.Ornaments)
        {
            string rowName = $"Ornament {ornament.RowId}";
            var entry = new GameDataAppearanceEntry(this, EntityManager, ornament.RowId, ornament.Singular ?? rowName, ornament.Icon, ornament, $"{ornament.RowId}");
            entry.Tags.Add("Ornament").WithAlias("Fashion Accessory");
            entry.SourceInfo = rowName;
            Add(entry);
        }
    }


    protected override string GetInternalId()
    {
        return "Ornaments";
    }
}
