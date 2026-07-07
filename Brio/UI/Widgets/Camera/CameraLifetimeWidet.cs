using Brio.Capabilities.Camera;
using Brio.UI.Controls.Stateless;
using Brio.UI.Widgets.Core;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace Brio.UI.Widgets.Camera;

public class CameraLifetimeWidget(CameraLifetimeCapability capability) : Widget<CameraLifetimeCapability>(capability)
{
    public override string HeaderName => "Lifetime";

    public override WidgetFlags Flags => WidgetFlags.DrawPopup | WidgetFlags.DrawQuickIcons;

    public override void DrawQuickIcons()
    {
        using(ImRaii.Disabled(Capability.IsAllowed == false))
        {
            if(ImBrio.FontIconButton("CameraLifetime_clone", FontAwesomeIcon.Clone, "Clone Camera"))
            {
                Capability.VirtualCameraManager.CloneCamera(Capability.CameraEntity.CameraID);
            }

            ImGui.SameLine();

            if(ImBrio.FontIconButton("CameraLifetime_target", FontAwesomeIcon.LocationCrosshairs, "Set as Active Camera"))
            {
                Capability.VirtualCameraManager.SelectCamera(Capability.VirtualCamera);
            }

            ImBrio.VerticalSeparator(24, 1);

            using(ImRaii.Disabled(Capability.CameraEntity.CameraID == 0))
            {
                if(ImBrio.HoldButton("CameraLifetime_destroy", "", FontAwesomeIcon.Trash, 1f, centerTest: true, tooltip: "[HOLD TO DESTROY]", onlyIcon: true))
                {
                    Capability.VirtualCameraManager.DestroyCamera(Capability.CameraEntity.CameraID);
                }

                ImBrio.VerticalSeparator(24, 1);

                if(ImBrio.FontIconButton("CameraLifetime_rename", FontAwesomeIcon.Signature, "Rename"))
                {
                    ModalManager.Instance.OpenRenameModal(Capability.Entity);
                }
            }

            ImGui.SameLine();

            var isLocked = Capability.Entity.IsLocked;
            var lockIcon = isLocked ? FontAwesomeIcon.Lock : FontAwesomeIcon.Unlock;
            if(ImBrio.ToggelFontIconButton("CameraLifetime_lock", lockIcon, new Vector2(25, 0), isLocked, tooltip: isLocked ? "Locked" : "Unlocked"))
            {
                Capability.Entity.IsLocked = !Capability.Entity.IsLocked;
            }
        }
    }

    public override void DrawPopup()
    {
        if(Capability.IsAllowed == false)
            return;

        using(ImRaii.Disabled(Capability.CameraEntity.IsDefaultCamera))
        {
            if(ImGui.MenuItem($"Rename {Capability.CameraEntity.FriendlyName}###CameraLifetime_rename"))
            {
                ImGui.CloseCurrentPopup();

                ModalManager.Instance.OpenRenameModal(Capability.Entity);
            }
        }

        if(ImGui.MenuItem("Clone###CameraLifetime_clone"))
        {
            Capability.VirtualCameraManager.CloneCamera(Capability.CameraEntity.CameraID);
        }

        if(ImGui.MenuItem("Target###CameraLifetime_target"))
        {
            Capability.VirtualCameraManager.SelectCamera(Capability.VirtualCamera);
        }

        var lockLabel = Capability.Entity.IsLocked ? "Unlock" : "Lock";
        if(ImGui.MenuItem($"{lockLabel}###CameraLifetime_lock"))
        {
            Capability.Entity.IsLocked = !Capability.Entity.IsLocked;
        }

        if(Capability.CanDestroy)
        {
            ImGui.Separator();

            if(ImGui.BeginMenu("Destroy###CameraLifetime_destroy"))
            {
                if(ImGui.MenuItem("Confirm Destruction###CameraLifetime_destroy_confirm"))
                {
                    Capability.VirtualCameraManager.DestroyCamera(Capability.CameraEntity.CameraID);
                }

                ImGui.EndMenu();
            }
        }
    }
}
