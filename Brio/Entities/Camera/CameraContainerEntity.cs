using Brio.Capabilities.Actor;
using Brio.Capabilities.Camera;
using Brio.Entities.Core;
using Brio.Game.Actor;
using Brio.Game.Camera;
using Brio.Game.Input;
using Brio.Game.World;
using Brio.UI.Controls.Editors;
using Brio.UI.Controls.Stateless;
using Brio.UI.Theming;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Brio.Entities.Camera;

public class CameraContainerEntity(IServiceProvider provider) : Entity("cameras", provider)
{
    private readonly VirtualCameraManager _virtualCameraManager = provider.GetRequiredService<VirtualCameraManager>();
    private readonly ActorSpawnService _actorSpawnService = provider.GetRequiredService<ActorSpawnService>();
    private readonly GameInputService _gameInputService = provider.GetRequiredService<GameInputService>();
    private readonly LightingService _lightingService = provider.GetRequiredService<LightingService>();

    public override string FriendlyName => "Entities";

    public override FontAwesomeIcon Icon => FontAwesomeIcon.GroupArrowsRotate;

    public override int ContextButtonCount => 2;

    public override EntityFlags Flags => EntityFlags.HasContextButton;

    public override void DrawContextButton()
    {
        using(ImRaii.PushColor(ImGuiCol.Button, ThemeManager.CurrentTheme.Accent.AccentColor))
        {
            var lockIcon = IsLocked ? FontAwesomeIcon.Lock : FontAwesomeIcon.Unlock;
            var lockToolTip = IsLocked ? "Unlock Cameras" : "Lock Cameras";
            if(ImBrio.FontIconButtonRight($"###{Id}_cameras_lock", lockIcon, 2f, lockToolTip, bordered: false))
            {
                IsLocked = !IsLocked;
            }

            ImGui.SameLine();

            string toolTip = $"New...";

            if(ImBrio.FontIconButtonRight($"###{Id}_cameras_contextButton", FontAwesomeIcon.Plus, 1f, toolTip, bordered: false))
            {
                ImGui.OpenPopup("UnifiedSpawnMenuPopup");
            }

            SpawnMenuEditor.DrawUnifiedSpawnMenu(_actorSpawnService, _virtualCameraManager, _lightingService);
        }
    }
    public override void OnAttached()
    {
        AddCapability(ActivatorUtilities.CreateInstance<CameraContainerCapability>(_serviceProvider, this));
    }

    public override void OnSelected()
    {
        _gameInputService.AllowEscape = true;
        base.OnSelected();
    }

    public override void OnChildAttached() => SortChildren();
    public override void OnChildDetached() => SortChildren();

    private void SortChildren() =>
        _children.Sort(static (a, b) => string.Compare(a.Id.Unique, b.Id.Unique, StringComparison.Ordinal));

}
