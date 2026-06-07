using Brio.Entities;
using Brio.Resources;

namespace Brio.Library.Sources;

public class GameDataCompanionSource : GameDataAppearanceSourceBase
{
    public GameDataCompanionSource(GameDataProvider lumina, EntityManager entityManager)
        : base(lumina, entityManager)
    {
    }

    public override string Name => "Minions";
    public override string Description => "Minions from FFXIV";

    public override void Scan()
    {
        foreach(var companion in Lumina.FilteredCompanions)
        {
            var hasName = Lumina.TryGetCompanionName(companion.RowId, out var name);
            var entry = new GameDataAppearanceEntry(this, EntityManager, companion.RowId, name, companion.Icon, companion, $"{companion.RowId}");

            entry.Tags.Add("Companion").WithAlias("Minion");

            if(hasName)
                entry.Tags.Add("Named");

            entry.SourceInfo = $"Companion {companion.RowId}";

            Add(entry);
        }
    }

    protected override string GetPublicId()
    {
        return "Companions";
    }
}
