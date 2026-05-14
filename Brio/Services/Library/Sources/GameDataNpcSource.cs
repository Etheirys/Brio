using Brio.Entities;
using Brio.Resources;
using System.Collections.Generic;

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
            string name = $"B:{npc.RowId:D7}";
            string? displayName = ResolveName(name);
            var entry = new GameDataAppearanceEntry(this, EntityManager, npc.RowId, displayName ?? name, 0, npc, $"B{npc.RowId}");
            entry.SourceInfo = $"BNpc {npc.RowId}";
            entry.Tags.Add("NPC");


            if(!string.IsNullOrEmpty(displayName))
                entry.Tags.Add("Named");

            Add(entry);
        }

        foreach(var npc in Lumina.ENpcBases)
        {
            string name = $"E:{npc.RowId:D7}";
            var displayName = Lumina.GetENpcName(npc.RowId);

            if(string.IsNullOrEmpty(displayName))
            {
                displayName = ResolveName(name);
            }

            var entry = new GameDataAppearanceEntry(this, EntityManager, npc.RowId, displayName ?? name, 0, npc, $"E{npc.RowId}");
            entry.SourceInfo = $"ENpc {npc.RowId}";
            entry.Tags.Add("NPC");

            if(!string.IsNullOrEmpty(displayName))
                entry.Tags.Add("Named");

            Add(entry);
        }
    }

    public string? ResolveName(string name)
    {
        var names = ResourceProvider.Instance.GetResourceDocument<IReadOnlyDictionary<string, string>>("Data.NpcNames.json");

        if(names.TryGetValue(name, out var nameOverride))
            name = nameOverride;

        if(name.StartsWith("N:"))
        {
            var nameId = uint.Parse(name.Substring(2));
            var bNpcName = Lumina.GetBNpcName(nameId);

            if(!string.IsNullOrEmpty(bNpcName))
            {
                return bNpcName;
            }
        }

        return null;
    }

    protected override string GetpublicId()
    {
        return "NPCs";
    }
}
