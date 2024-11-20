using Brio.Entities;
using Brio.Resources;

namespace Brio.Library.Sources;

internal class GameDataCompanionSource : GameDataAppearanceSourceBase
{
    public GameDataCompanionSource(GameDataProvider lumina, EntityManager entityManager)
        : base(lumina, entityManager)
    {
    }

    public override string Name => "Minions";
    public override string Description => "Minions from FFXIV";

    public override void Scan()
    {
        foreach(var (_, companion) in Lumina.Companions)
        {
            string rowName = $"Companion {companion.RowId}";
            var entry = new GameDataAppearanceEntry(this, EntityManager, companion.RowId, companion.Singular.ToString() ?? rowName, companion.Icon, companion, $"{companion.RowId}");
            entry.Tags.Add("Companion").WithAlias("Minion");
            entry.SourceInfo = rowName;
            Add(entry);
        }
    }

    protected override string GetInternalId()
    {
        return "Companions";
    }
}
