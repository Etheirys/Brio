using Brio.Capabilities.World;
using Brio.Entities.Core;
using Brio.Game.GPose;
using Brio.Game.World;
using Brio.UI.Controls.Editors;
using Brio.UI.Controls.Stateless;
using Brio.UI.Theming;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Brio.Entities.World;

public class EnvironmentContainerEntity(IServiceProvider provider) : Entity("environment", provider)
{
    private readonly GPoseService _gPoseService = provider.GetRequiredService<GPoseService>();
    private readonly LightingService _lightingService = provider.GetRequiredService<LightingService>();

    public override string FriendlyName => "Environment";
    public override FontAwesomeIcon Icon => FontAwesomeIcon.MountainSun;

    public override int ContextButtonCount => 1;
    public override EntityFlags Flags => EntityFlags.AllowOutsideGpose | EntityFlags.DefaultOpen | EntityFlags.HasContextButton;

    public override void DrawContextButton()
    {
        using(ImRaii.Disabled(_gPoseService.IsGPosing == false))
        {
            using(ImRaii.PushColor(ImGuiCol.Button, ThemeManager.CurrentTheme.Accent.AccentColor))
            {
                string toolTip = $"New Light";
                if(ImBrio.FontIconButtonRight($"###{Id}_light_contextButton", FontAwesomeIcon.Plus, 1f, toolTip, bordered: false))
                {
                    ImGui.OpenPopup("DrawLightSpawnMenuPopup");
                }
                LightEditor.DrawSpawnMenu(_lightingService);
            }
        }
    }

    public override void OnAttached()
    {
        AddCapability(ActivatorUtilities.CreateInstance<LightContainerCapability>(_serviceProvider, this));
        AddCapability(ActivatorUtilities.CreateInstance<TimeWeatherCapability>(_serviceProvider, this));
        AddCapability(ActivatorUtilities.CreateInstance<FestivalCapability>(_serviceProvider, this));
        AddCapability(ActivatorUtilities.CreateInstance<WorldRenderingCapability>(_serviceProvider, this));
    }

    public override void OnChildAttached() => SortChildren();
    public override void OnChildDetached() => SortChildren();

    private void SortChildren()
    {
        _children.Sort((a, b) =>
        {
            if(a is LightEntity actorA && b is LightEntity actorB)
                return actorA.GameLight.Index.CompareTo(actorB.GameLight.Index);

            return string.Compare(a.Id.Unique, b.Id.Unique, StringComparison.Ordinal);
        });
    }
}
