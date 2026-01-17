using Brio.Capabilities.Camera;
using Brio.Game.Actor;
using Brio.Game.World;
using Brio.UI.Controls;
using Brio.UI.Controls.Editors;
using Brio.UI.Controls.Stateless;
using Brio.UI.Widgets.Core;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

namespace Brio.UI.Widgets.Camera;

public class CameraLifetimeWidget : Widget<CameraLifetimeCapability>
{
    private readonly ActorSpawnService _actorSpawnService;
    private readonly LightingService _lightingService;

    public CameraLifetimeWidget(CameraLifetimeCapability capability, ActorSpawnService actorSpawnService, LightingService lightingService) : base(capability)
    {
        _actorSpawnService = actorSpawnService;
        _lightingService = lightingService;
    }

    public override string HeaderName => "Lifetime";

    public override WidgetFlags Flags => WidgetFlags.DrawPopup | WidgetFlags.DrawQuickIcons;

    public override void DrawQuickIcons()
    {
        using(ImRaii.Disabled(Capability.IsAllowed == false))
        {
            if(ImBrio.FontIconButton("CameraLifetime_spawnnew", FontAwesomeIcon.Plus, "Spawn New"))
            {
                ImGui.OpenPopup("UnifiedSpawnMenuPopup");
            }
            SpawnMenuEditor.DrawUnifiedSpawnMenu(_actorSpawnService, Capability.VirtualCameraManager, _lightingService);

            ImGui.SameLine();

            if(ImBrio.FontIconButton("CameraLifetime_clone", FontAwesomeIcon.Clone, "Clone Camera"))
            {
                Capability.VirtualCameraManager.CloneCamera(Capability.CameraEntity.CameraID);
            }

            ImGui.SameLine();

            using(ImRaii.Disabled(Capability.CameraEntity.CameraID == 0))
            {
                if(ImBrio.FontIconButton("CameraLifetime_destroy", FontAwesomeIcon.Trash, "Destroy Camera", Capability.CanDestroy))
                {
                    Capability.VirtualCameraManager.DestroyCamera(Capability.CameraEntity.CameraID);
                }

                ImGui.SameLine();

                if(ImBrio.FontIconButton("CameraLifetime_rename", FontAwesomeIcon.Signature, "Rename"))
                {
                    RenameActorModal.Open(Capability.Entity);
                }
            }

            ImGui.SameLine();

            if(ImBrio.FontIconButton("CameraLifetime_target", FontAwesomeIcon.Bullseye, "Target Camera"))
            {
                Capability.VirtualCameraManager.SelectCamera(Capability.VirtualCamera);
            }

        }
    }

    public override void DrawPopup()
    {
        if(Capability.IsAllowed == false)
            return;

        if(ImGui.MenuItem("Target###CameraLifetime_target"))
        {
            Capability.VirtualCameraManager.SelectCamera(Capability.VirtualCamera);
        }

        if(ImGui.MenuItem("Clone###CameraLifetime_clone"))
        {
            Capability.VirtualCameraManager.CloneCamera(Capability.CameraEntity.CameraID);
        }

        if(Capability.CanDestroy)
        {
            if(ImGui.BeginMenu("Destroy###actorlifetime_destroy"))
            {
                if(ImGui.MenuItem("Confirm Destruction###CameraLifetime_destroy_confirm"))
                {
                    Capability.VirtualCameraManager.DestroyCamera(Capability.CameraEntity.CameraID);
                }

                ImGui.EndMenu();
            }


            if(ImGui.MenuItem($"Rename {Capability.CameraEntity.FriendlyName}###CameraLifetime_rename"))
            {
                ImGui.CloseCurrentPopup();

                RenameActorModal.Open(Capability.Entity);
            }
        }
    }
}
