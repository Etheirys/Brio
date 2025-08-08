using Brio.Capabilities.Camera;
using Brio.UI.Controls;
using Brio.UI.Controls.Editors;
using Brio.UI.Controls.Stateless;
using Brio.UI.Widgets.Core;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;

namespace Brio.UI.Widgets.Camera;

public class CameraLifetimeWidget(CameraLifetimeCapability capability) : Widget<CameraLifetimeCapability>(capability)
{
    public override string HeaderName => "Lifetime";

    public override WidgetFlags Flags => WidgetFlags.DrawPopup | WidgetFlags.DrawQuickIcons;

    public override void DrawQuickIcons()
    {
        using(ImRaii.Disabled(Capability.IsAllowed == false))
        {
            if(ImBrio.FontIconButton("CameraLifetime_spawnnew", FontAwesomeIcon.Plus, "New Camera"))
            {
                ImGui.OpenPopup("DrawSpawnMenuPopup");
            }
            CameraEditor.DrawSpawnMenu(Capability.VirtualCameraManager);

            ImGui.SameLine();

            if(ImBrio.FontIconButton("CameraLifetime_clone", FontAwesomeIcon.Clone, "Clone Camera", Capability.CanClone))
            {
                Capability.VirtualCameraManager.CloneCamera(Capability.CameraEntity.CameraID);
            }

            ImGui.SameLine();

            if(ImBrio.FontIconButton("CameraLifetime_target", FontAwesomeIcon.Bullseye, "Target Camera"))
            {
                Capability.VirtualCameraManager.SelectCamera(Capability.VirtualCamera);
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

        if(Capability.CanClone)
        {
            if(ImGui.MenuItem("Clone###CameraLifetime_clone"))
            {
                Capability.VirtualCameraManager.CloneCamera(Capability.CameraEntity.CameraID);
            }
        }

        if(Capability.CanDestroy)
        {
            if(ImGui.MenuItem("Destroy###CameraLifetime_destroy"))
            {
                Capability.VirtualCameraManager.DestroyCamera(Capability.CameraEntity.CameraID);
            }

            if(ImGui.MenuItem($"Rename {Capability.CameraEntity.FriendlyName}###CameraLifetime_rename"))
            {
                ImGui.CloseCurrentPopup();

                RenameActorModal.Open(Capability.Entity);
            }
        }
    }
}
