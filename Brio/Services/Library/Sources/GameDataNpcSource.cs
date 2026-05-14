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
        foreach(var npc in Lumina.BNpcBases)
        {
            string rowName = $"B:{npc.RowId:D7}";
            string? displayName = Lumina.GetBNpcName(npc.RowId);
            var entry = new GameDataAppearanceEntry(this, EntityManager, npc.RowId, displayName, 0, npc, $"B{npc.RowId}");
            entry.SourceInfo = $"BNpc {npc.RowId}";
            entry.Tags.Add("NPC");

            if(!string.IsNullOrEmpty(displayName) && displayName != rowName)
                entry.Tags.Add("Named");

            Add(entry);
        }

        foreach(var npc in Lumina.ENpcBases)
        {
            string rowName = $"E:{npc.RowId:D7}";
            var displayName = Lumina.GetENpcName(npc.RowId);
            var entry = new GameDataAppearanceEntry(this, EntityManager, npc.RowId, displayName ?? rowName, 0, npc, $"E{npc.RowId}");
            entry.SourceInfo = $"ENpc {npc.RowId}";
            entry.Tags.Add("NPC");

            if(!string.IsNullOrEmpty(displayName) && displayName != rowName)
                entry.Tags.Add("Named");

            Add(entry);
        }
    }

    protected override string GetpublicId()
    {
        return "NPCs";
    }
}
