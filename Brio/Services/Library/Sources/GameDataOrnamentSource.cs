using Brio.Entities;
using Brio.Resources;

namespace Brio.Library.Sources;

public class GameDataOrnamentSource : GameDataAppearanceSourceBase
{
    public GameDataOrnamentSource(GameDataProvider lumina, EntityManager entityManager)
        : base(lumina, entityManager)
    {
    }

    public override string Name => "Fashion Accessory";
    public override string Description => "Fashion Accessories from FFXIV";

    public override void Scan()
    {
        foreach(var ornament in Lumina.FilteredOrnaments)
        {
            var hasName = GameDataProvider.Instance.TryGetOrnamentName(ornament.RowId, out var name);
            var entry = new GameDataAppearanceEntry(this, EntityManager, ornament.RowId, name, ornament.Icon, ornament, ornament.RowId.ToString());

            entry.Tags.Add("Ornament").WithAlias("Fashion Accessory");

            if(hasName)
                entry.Tags.Add("Named");

            entry.SourceInfo = $"Ornament {ornament.RowId}";

            Add(entry);
        }
    }

    protected override string GetPublicId()
    {
        return "Ornaments";
    }
}
