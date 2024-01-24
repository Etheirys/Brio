using Brio.Resources;
using System.Collections.Generic;

namespace Brio.Library.Sources;

internal class GameDataNpcSource : SourceBase
{
    private GameDataProvider _lumina;

    public GameDataNpcSource(GameDataProvider lumina)
        : base("NPCs", ResourceProvider.Instance.GetResourceImage("Images.ProviderIcon_GameData.png"))
    {
        _lumina = lumina;
    }

    public override void Scan()
    {
        foreach(var (_, npc) in _lumina.BNpcBases)
        {
            string name = $"B:{npc.RowId:D7}";
            name = ResolveName(name);
            var entry = new GameDataAppearanceEntry(this, name, 0, npc);
            entry.SourceInfo = $"BNpc {npc.RowId}";
            entry.Tags.Add("NPC");


            if(!string.IsNullOrEmpty(name))
                entry.Tags.Add("Named");

            Add(entry);
        }

        foreach(var (_, npc) in _lumina.ENpcBases)
        {
            string name = $"E:{npc.RowId:D7}";

            var resident = _lumina.ENpcResidents[npc.RowId];
            if(resident != null)
            {
                if(!string.IsNullOrEmpty(resident.Singular))
                    name = resident.Singular;
            }

            name = ResolveName(name);
            var entry = new GameDataAppearanceEntry(this, name, 0, npc);
            entry.SourceInfo = $"ENpc {npc.RowId}";
            entry.Tags.Add("NPC");

            if(!string.IsNullOrEmpty(name))
                entry.Tags.Add("Named");

            Add(entry);
        }
    }

    public static string ResolveName(string name)
    {
        var names = ResourceProvider.Instance.GetResourceDocument<IReadOnlyDictionary<string, string>>("Data.NpcNames.json");

        if(names.TryGetValue(name, out var nameOverride))
            name = nameOverride;

        if(name.StartsWith("N:"))
        {
            var nameId = uint.Parse(name.Substring(2));
            if(GameDataProvider.Instance.BNpcNames.TryGetValue(nameId, out var nameRef))
                if(nameRef != null && !string.IsNullOrEmpty(nameRef.Singular))
                    name = nameRef.Singular;
        }

        return name;
    }
}
