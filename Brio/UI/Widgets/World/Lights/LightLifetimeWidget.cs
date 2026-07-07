using Brio.Capabilities.World;
using Brio.UI.Controls.Stateless;
using Brio.UI.Widgets.Core;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;

namespace Brio.UI.Widgets.World.Lights;

public class LightLifetimeWidget(LightLifetimeCapability lightLifetimeCapability) : Widget<LightLifetimeCapability>(lightLifetimeCapability)
{
    public override string HeaderName => "Lifetime";
    public override WidgetFlags Flags => WidgetFlags.DrawPopup | WidgetFlags.DrawQuickIcons;

    public override void DrawQuickIcons()
    {
        if(ImBrio.FontIconButton("lifetimewidget_clone", FontAwesomeIcon.Clone, "Clone Light", Capability.CanClone))
        {
            Capability.Clone();
        }

        ImGui.SameLine();

        if(ImBrio.FontIconButton("lifetimewidget_move_to_camera", FontAwesomeIcon.CaretSquareDown, "Move to Camera"))
        {
            Capability.MoveToCamera();
        }

        ImBrio.VerticalSeparator(24, 1);

        if(ImBrio.HoldButton("lifetimewidget_destroy", "", FontAwesomeIcon.Trash, 1f, new(40, 0), centerTest: true, tooltip: "[HOLD TO DESTROY]", onlyIcon: true))
        {
            Capability.Destroy();
        }

        ImBrio.VerticalSeparator(24, 1);

        if(ImBrio.FontIconButton("lifetimewidget_rename", FontAwesomeIcon.Signature, "Rename Light"))
        {
            ModalManager.Instance.OpenRenameModal(Capability.Entity);
        }
    }

    public override void DrawPopup()
    {
        if(ImGui.MenuItem($"Rename {Capability.Entity.FriendlyName}###lightlifetime_rename"))
        {
            ImGui.CloseCurrentPopup();

            ModalManager.Instance.OpenRenameModal(Capability.Entity);
        }

        if(Capability.CanClone)
        {
            if(ImGui.MenuItem("Clone###lightlifetime_clone"))
            {
                Capability.Clone();
            }
        }

        if(ImGui.MenuItem("Move to Camera###lightlifetime_move_to_camera"))
        {
            Capability.MoveToCamera();
        }

        var togglenText = Capability.GameLight.IsVisible ? $"Turn OFF {Capability.Entity.FriendlyName}" : $"Turn ON {Capability.Entity.FriendlyName}";
        if(ImGui.MenuItem($"{togglenText}###lightlifetime_toggle"))
        {
            Capability.GameLight.ToggleLight();
        }

        if(ImGui.MenuItem("Open Light Window###lightlifetime_lightwindow"))
        {
            Capability.OpenLightWindow();
        }

        if(Capability.CanDestroy)
        {
            ImGui.Separator();

            if(ImGui.BeginMenu("Destroy###lightlifetime_destroy"))
            {
                if(ImGui.MenuItem("Confirm Destruction###lightlifetime_destroy_confirm"))
                {
                    Capability.Destroy();
                }

                ImGui.EndMenu();
            }
        }
    }
}
