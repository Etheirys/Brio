using Brio.Entities;
using Brio.Resources;

namespace Brio.Library.Sources;

public class GameDataNpcSource : GameDataAppearanceSourceBase
{
    public GameDataNpcSource(GameDataProvider lumina, EntityManager entityManager)
        : base(lumina, entityManager)
    {
    }

    public override string Name => "NPCs";
    public override string Description => "Non Player Characters from FFXIV";

    public override void Scan()
    {
        foreach(var row in Lumina.FilteredBNpcBases)
        {
            var hasName = Lumina.TryGetBNpcNameByBase(row.RowId, out var name);
            var entry = new GameDataAppearanceEntry(this, EntityManager, row.RowId, name, 0, row, $"B{row.RowId}");

            entry.Tags.Add("NPC");

            if(hasName)
                entry.Tags.Add("Named");

            entry.SourceInfo = $"BNpc {row.RowId}";

            Add(entry);
        }

        foreach(var row in Lumina.FilteredENpcBases)
        {
            var hasName = Lumina.TryGetENpcName(row.RowId, out var name);
            var entry = new GameDataAppearanceEntry(this, EntityManager, row.RowId, name, 0, row, $"E{row.RowId}");

            entry.Tags.Add("NPC");

            if(hasName)
                entry.Tags.Add("Named");

            entry.SourceInfo = $"ENpc {row.RowId}";

            Add(entry);
        }
    }

    protected override string GetPublicId()
    {
        return "NPCs";
    }
}
