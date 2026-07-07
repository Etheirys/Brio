using Brio.Capabilities.Timeline;
using Brio.Capabilities.WorldObjects;
using Brio.Core;
using Brio.Entities.Core;
using Brio.Game.WorldObjects;
using Brio.UI;
using Brio.UI.Controls.Stateless;
using Brio.UI.Theming;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Brio.Entities.WorldObjects;

public class WorldObjectEntity(IWorldObject worldObject, IServiceProvider provider) : TransformableEntity(new EntityId(worldObject), provider), ITransformable
{
    public IWorldObject GameBgObject => worldObject;

    public string RawName = "";

    public override string FriendlyName
    {
        get => string.IsNullOrEmpty(RawName)
            ? (!string.IsNullOrEmpty(worldObject.FriendlyPath) ? $"{worldObject.FriendlyName} [{worldObject.FriendlyPath}]" : worldObject.FriendlyName)
            : RawName;
        set => RawName = value;
    }

    public override FontAwesomeIcon Icon => worldObject.ObjectType switch
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
        => ModalManager.Instance.OpenRenameModal(this);

    public override void Snapshot()
    => Transformable?.Snapshot();

    public override void DrawContextButton()
    {
        using(ImRaii.PushColor(ImGuiCol.Button, ThemeManager.CurrentTheme.Accent.AccentColor, !worldObject.IsVisible))
        {
            string toolTip = worldObject.IsVisible ? $"Hide {FriendlyName}" : $"Show {FriendlyName}";
            if(ImBrio.FontIconButtonRight($"###{Id}_hideObj", worldObject.IsVisible ? FontAwesomeIcon.Eye : FontAwesomeIcon.EyeSlash, 1f, toolTip, bordered: false))
                worldObject.IsVisible = !worldObject.IsVisible;
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

        AddCapability(ActivatorUtilities.CreateInstance<WorldObjectTimelineCapability>(_serviceProvider, this));
    }
}
