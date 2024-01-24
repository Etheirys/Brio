using Brio.Game.Types;
using Brio.Resources;
using Brio.UI;
using Dalamud.Interface.Internal;
using System;

namespace Brio.Library.Sources;

internal class GameDataAppearanceEntry : LibraryEntryBase
{
    private string _name;
    private uint _icon;
    private ActorAppearanceUnion _appearance;

    public GameDataAppearanceEntry(SourceBase source, string name, uint icon, ActorAppearanceUnion appearance)
        : base(source)
    {
        _name = name;
        _icon = icon;
        _appearance = appearance;

        Author = "Square Enix";
    }

    public override string Name => _name;
    public override Type? FileType => typeof(ActorAppearanceUnion);

    public override IDalamudTextureWrap? Icon
    {
        get
        {
            if(_icon <= 0)
                return ResourceProvider.Instance.GetResourceImage("Images.FileIcon_Chara.png");

            return UIManager.Instance.TextureProvider.GetIcon(_icon);
        }
    }

    public override IDalamudTextureWrap? PreviewImage
    {
        get
        {
            if(_icon <= 0)
                return null;

            return UIManager.Instance.TextureProvider.GetIcon(_icon);
        }
    }
}
