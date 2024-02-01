using Brio.Resources;

namespace Brio.Library.Sources;

internal class GameDataCompanionSource : GameDataAppearanceSourceBase
{
    public GameDataCompanionSource(LibraryManager manager, GameDataProvider lumina)
        : base(manager, lumina)
    {
    }

    public override string Name => "Minions";
    public override string Description => "Minions from FFXIV";

    public override void Scan()
    {
        foreach(var (_, companion) in Lumina.Companions)
        {
            string rowName = $"Companion {companion.RowId}";
            var entry = new GameDataAppearanceEntry(this, companion.RowId, companion.Singular ?? rowName, companion.Icon, companion);
            entry.Tags.Add("Companion").WithAlias("Minion");
            entry.SourceInfo = rowName;
            Add(entry);
        }
    }

   
}
