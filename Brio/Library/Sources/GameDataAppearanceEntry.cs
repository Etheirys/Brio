using Brio.Capabilities.Actor;
using Brio.Entities;
using Brio.Game.Actor.Appearance;
using Brio.Game.Types;
using Brio.Resources;
using Brio.UI;
using Brio.UI.Controls.Stateless;
using Dalamud.Interface.Internal;
using System;

namespace Brio.Library.Sources;

internal class GameDataAppearanceEntry : ItemEntryBase
{
    private string _name;
    private uint _icon;
    private ActorAppearanceUnion _appearance;
    private uint _rowId;
    private string _id;
    private EntityManager _entityManager;

    public GameDataAppearanceEntry(SourceBase source, EntityManager entityManager, uint rowId, string name, uint icon, ActorAppearanceUnion appearance, string id)
        : base(source)
    {
        _name = name;
        _icon = icon;
        _appearance = appearance;
        _rowId = rowId;
        _id = id;
        _entityManager = entityManager;

        ActorAppearance app = _appearance;

        if(app.ModelCharaId == 0)
        {
            Tags.Add("Human");

            if(app.Customize.Race != 0)
                Tags.Add(app.Customize.Race.ToDisplayName());

            if(app.Customize.Tribe != 0)
                Tags.Add(app.Customize.Tribe.ToDisplayName());

            Tags.Add(app.Customize.Gender.ToDisplayName());
        }
        else
        {
            Tags.Add("Inhuman");
        }
        
    }

    public override string Name => _name;
    public override string? Author => "Square Enix";
    public override Type LoadsType => typeof(ActorAppearanceUnion);

    public ActorAppearanceUnion Appearance => _appearance;

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

    public override object? Load()
    {
        return _appearance;
    }

    public override bool Search(string[] query)
    {
        bool match = base.Search(query);
        match |= SearchUtility.Matches(this._rowId, query);
        return match;
    }

    protected override string GetInternalId() => _id;

    public override void DrawActions(bool isModal)
    {
        base.DrawActions(isModal);

        ImBrio.DrawApplyToActor(_entityManager, (actor) =>
        {
            ActorAppearanceCapability? capability;
            if(actor.TryGetCapability<ActorAppearanceCapability>(out capability) && capability != null)
            {
                _ = capability.SetAppearance(Appearance, AppearanceImportOptions.All);
            }
        });
    }
}
