using Brio.Resources;

namespace Brio.Library.Sources;

internal class GameDataMountSource : SourceBase
{
    private GameDataProvider _lumina;

    public GameDataMountSource(GameDataProvider lumina)
        : base("Mounts", ResourceProvider.Instance.GetResourceImage("Images.ProviderIcon_GameData.png"))
    {
        _lumina = lumina;
    }

    public override void Scan()
    {
        foreach(var (_, mount) in _lumina.Mounts)
        {
            string rowName = $"Mount {mount.RowId}";

            string name = mount.Singular;
            if(string.IsNullOrEmpty(name))
                name = rowName;

            var entry = new GameDataAppearanceEntry(this, name, mount.Icon, mount);
            entry.Tags.Add("Mount");

            if (name != rowName)
                entry.Tags.Add("Named");

            entry.SourceInfo = rowName;
            Add(entry);
        }
    }
}
