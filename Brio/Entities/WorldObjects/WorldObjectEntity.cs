using Brio.Capabilities.WorldObjects;
using Brio.Core;
using Brio.Entities.Core;
using Brio.Game.WorldObjects;
using Brio.UI.Controls;
using Brio.UI.Controls.Stateless;
using Brio.UI.Theming;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;

namespace Brio.Entities.WorldObjects;

public class WorldObjectEntity(IWorldObject gameObject, IServiceProvider provider) : TransformableEntity(new EntityId(gameObject), provider), ITransformable
{
    public IWorldObject GameBgObject => gameObject;

    public string RawName = "";

    public override string FriendlyName
    {
        get => string.IsNullOrEmpty(RawName)
            ? $"{gameObject.FriendlyName}: {Path.GetFileNameWithoutExtension(gameObject.Path)}"
            : RawName;
        set => RawName = value;
    }

    public override FontAwesomeIcon Icon => gameObject.ObjectType switch
    {
        WorldObjectType.BgObject => FontAwesomeIcon.Box,
        WorldObjectType.StaticVfx => FontAwesomeIcon.Burst,
        WorldObjectType.Prop => FontAwesomeIcon.Cube,
        WorldObjectType.Furniture => FontAwesomeIcon.Chair,
        _ => FontAwesomeIcon.Question
    };

    public override bool IsVisible => true;
    public override EntityFlags Flags => EntityFlags.AllowDoubleClick | EntityFlags.HasContextButton | EntityFlags.DefaultOpen | EntityFlags.AllowMultiSelect;
    public override int ContextButtonCount => 1;

    public override void OnDoubleClick() 
        => RenameActorModal.Open(this);

    public override void Snapshot()
    => Transformable?.Snapshot();

    public override void DrawContextButton()
    {
        using(ImRaii.PushColor(ImGuiCol.Button, ThemeManager.CurrentTheme.Accent.AccentColor, !gameObject.IsVisible))
        {
            string toolTip = gameObject.IsVisible ? $"Hide {FriendlyName}" : $"Show {FriendlyName}";
            if(ImBrio.FontIconButtonRight($"###{Id}_hideObj", gameObject.IsVisible ? FontAwesomeIcon.Eye : FontAwesomeIcon.EyeSlash, 1f, toolTip, bordered: false))
                gameObject.IsVisible = !gameObject.IsVisible;
        }
    }

    public override void SetVisibility(bool visible)
    {
        GameBgObject.IsVisible = visible;
    }

    public override void OnAttached()
    {
        AddCapability(ActivatorUtilities.CreateInstance<WorldObjectLifetimeCapability>(_serviceProvider, this));

        AddTransformable<WorldObjectTransformCapability>();

        AddCapability(ActivatorUtilities.CreateInstance<DebugWorldObjectCapability>(_serviceProvider, this));
    }
}
