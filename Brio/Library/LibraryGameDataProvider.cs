using Brio.Game.Types;
using Brio.Resources;
using Brio.UI;
using Dalamud.Interface.Internal;
using System;
using System.Collections.Generic;

namespace Brio.Library;

internal class LibraryGameDataNpcProvider : LibraryProviderBase
{
    private GameDataProvider _lumina;

    public LibraryGameDataNpcProvider(GameDataProvider lumina)
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
            var entry = new NpcEntry(name, 0, npc);
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
            var entry = new NpcEntry(name, 0, npc);
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

internal class LibraryGameDataMountProvider : LibraryProviderBase
{
    private GameDataProvider _lumina;

    public LibraryGameDataMountProvider(GameDataProvider lumina)
        : base("Mounts", ResourceProvider.Instance.GetResourceImage("Images.ProviderIcon_GameData.png"))
    {
        _lumina = lumina;
    }

    public override void Scan()
    {
        foreach(var (_, mount) in _lumina.Mounts)
        {
            var entry = new NpcEntry(mount.Singular ?? $"Mount {mount.RowId}", mount.Icon, mount);
            entry.Tags.Add("Mount");
            Add(entry);
        }
    }
}

internal class LibraryGameDataCompanionsProvider : LibraryProviderBase
{
    private GameDataProvider _lumina;

    public LibraryGameDataCompanionsProvider(GameDataProvider lumina)
        : base("Companions", ResourceProvider.Instance.GetResourceImage("Images.ProviderIcon_GameData.png"))
    {
        _lumina = lumina;
    }

    public override void Scan()
    {
        foreach(var (_, companion) in _lumina.Companions)
        {
            var entry = new NpcEntry(companion.Singular ?? $"Companion {companion.RowId}", companion.Icon, companion);
            entry.Tags.Add("Companion").WithAlias("Minion");
            Add(entry);
        }
    }
}

internal class LibraryGameDataOrnamentsProvider : LibraryProviderBase
{
    private GameDataProvider _lumina;

    public LibraryGameDataOrnamentsProvider(GameDataProvider lumina)
        : base("Ornaments", ResourceProvider.Instance.GetResourceImage("Images.ProviderIcon_GameData.png"))
    {
        _lumina = lumina;
    }

    public override void Scan()
    {
        foreach(var (_, ornament) in _lumina.Ornaments)
        {
            var entry = new NpcEntry(ornament.Singular ?? $"Ornament {ornament.RowId}", ornament.Icon, ornament);
            entry.Tags.Add("Ornament").WithAlias("Fashion Accessory");
            Add(entry);
        }
    }
}

internal class NpcEntry : LibraryEntryBase
{
    private string _name;
    private uint _icon;
    private ActorAppearanceUnion _appearance;

    public NpcEntry(string name, uint icon, ActorAppearanceUnion appearance)
    {
        _name = name;
        _icon = icon;
        _appearance = appearance;
    }

    public override string Name => _name;
    public override Type? FileType => typeof(ActorAppearanceUnion);

    public override IDalamudTextureWrap? Icon
    {
        get
        {
            if (_icon <= 0)
                return ResourceProvider.Instance.GetResourceImage("Images.FileIcon_Chara.png");

            return UIManager.Instance.TextureProvider.GetIcon(_icon);
        }
    }
}
