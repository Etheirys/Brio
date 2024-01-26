using Brio.Resources;
using Dalamud.Interface.Internal;

namespace Brio.Library.Sources;

internal class GameDataMountSource : SourceBase
{
    private GameDataProvider _lumina;

    public GameDataMountSource(GameDataProvider lumina)
        : base()
    {
        _lumina = lumina;
    }

    public override string Name => "Mounts";
    public override IDalamudTextureWrap? Icon => ResourceProvider.Instance.GetResourceImage("Images.ProviderIcon_GameData.png");
    public override string Description => "Mounts from FFXIV";

    public override void Scan()
    {
        foreach(var (_, mount) in _lumina.Mounts)
        {
            string rowName = $"Mount {mount.RowId}";

            string name = mount.Singular;
            if(string.IsNullOrEmpty(name))
                name = rowName;

            var entry = new GameDataAppearanceEntry(this, mount.RowId, name, mount.Icon, mount);
            entry.Tags.Add("Mount");

            if (name != rowName)
                entry.Tags.Add("Named");

            entry.SourceInfo = rowName;
            Add(entry);
        }
    }
}
