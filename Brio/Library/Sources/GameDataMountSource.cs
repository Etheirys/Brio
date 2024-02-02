using Brio.Entities;
using Brio.Resources;

namespace Brio.Library.Sources;

internal class GameDataMountSource : GameDataAppearanceSourceBase
{
    public GameDataMountSource(GameDataProvider lumina, EntityManager entityManager)
        : base(lumina, entityManager)
    {
    }

    public override string Name => "Mounts";
    public override string Description => "Mounts from FFXIV";

    public override void Scan()
    {
        foreach(var (_, mount) in Lumina.Mounts)
        {
            string rowName = $"Mount {mount.RowId}";

            string name = mount.Singular;
            if(string.IsNullOrEmpty(name))
                name = rowName;

            var entry = new GameDataAppearanceEntry(this, EntityManager, mount.RowId, name, mount.Icon, mount, $"{mount.RowId}");
            entry.Tags.Add("Mount");

            if (name != rowName)
                entry.Tags.Add("Named");

            entry.SourceInfo = rowName;
            Add(entry);
        }
    }

    protected override string GetInternalId()
    {
        return "Mounts";
    }
}
