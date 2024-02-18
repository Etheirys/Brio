using Brio.Entities;
using Brio.Resources;
using System.Collections.Generic;

namespace Brio.Library.Sources;

internal class GameDataNpcSource : GameDataAppearanceSourceBase
{
    public GameDataNpcSource(GameDataProvider lumina, EntityManager entityManager)
        : base(lumina, entityManager)
    {
    }

    public override string Name => "NPCs";
    public override string Description => "Non Player Characters from FFXIV";

    public override void Scan()
    {
        foreach(var (_, npc) in Lumina.BNpcBases)
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

        foreach(var (_, npc) in Lumina.ENpcBases)
        {
            string name = $"E:{npc.RowId:D7}";
            string? displayName = null;

            var resident = Lumina.ENpcResidents[npc.RowId];
            if(resident != null && !string.IsNullOrEmpty(resident.Singular))
            {
                displayName = resident.Singular;
            }
            else
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

    public static string? ResolveName(string name)
    {
        var names = ResourceProvider.Instance.GetResourceDocument<IReadOnlyDictionary<string, string>>("Data.NpcNames.json");

        if(names.TryGetValue(name, out var nameOverride))
            name = nameOverride;

        if(name.StartsWith("N:"))
        {
            var nameId = uint.Parse(name.Substring(2));
            if(GameDataProvider.Instance.BNpcNames.TryGetValue(nameId, out var nameRef))
            {
                if(nameRef != null && !string.IsNullOrEmpty(nameRef.Singular))
                {
                    return nameRef.Singular;
                }
            }
        }

        return null;
    }

    protected override string GetInternalId()
    {
        return "NPCs";
    }
}
