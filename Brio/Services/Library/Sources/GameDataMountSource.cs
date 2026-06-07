using Brio.Entities;
using Brio.Resources;

namespace Brio.Library.Sources;

public class GameDataMountSource : GameDataAppearanceSourceBase
{
    public GameDataMountSource(GameDataProvider lumina, EntityManager entityManager)
        : base(lumina, entityManager)
    {
    }

    public override string Name => "Mounts";
    public override string Description => "Mounts from FFXIV";

    public override void Scan()
    {
        foreach(var mount in Lumina.FilteredMounts)
        {
            var hasName = Lumina.TryGetMountName(mount.RowId, out var name);
            var entry = new GameDataAppearanceEntry(this, EntityManager, mount.RowId, name, mount.Icon, mount, $"{mount.RowId}");

            entry.Tags.Add("Mount");

            if(hasName)
                entry.Tags.Add("Named");

            entry.SourceInfo = $"Mount {mount.RowId}";

            Add(entry);
        }
    }

    protected override string GetPublicId()
    {
        return "Mounts";
    }
}
