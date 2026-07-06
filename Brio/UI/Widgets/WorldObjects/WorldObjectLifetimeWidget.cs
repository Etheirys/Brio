using Brio.Capabilities.WorldObjects;
using Brio.UI.Controls;
using Brio.UI.Controls.Stateless;
using Brio.UI.Widgets.Core;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;

namespace Brio.UI.Widgets.WorldObjects;

public class WorldObjectLifetimeWidget(WorldObjectLifetimeCapability capability) : Widget<WorldObjectLifetimeCapability>(capability)
{
    public override string HeaderName => "Lifetime";
    public override WidgetFlags Flags => WidgetFlags.DrawPopup | WidgetFlags.DrawQuickIcons;

    public override void DrawQuickIcons()
    {
        if(ImBrio.FontIconButton("bglifetime_clone", FontAwesomeIcon.Clone, "Clone", Capability.CanClone))
        {
            Capability.Clone();
        }

        ImGui.SameLine();

        if(ImBrio.FontIconButton("bglifetime_movetocamera", FontAwesomeIcon.CaretSquareDown, "Move to Camera"))
        {
            Capability.MoveToCamera();
        }

        ImBrio.VerticalSeparator(24, 1);

        if(ImBrio.HoldButton("bglifetime_destroy", "", FontAwesomeIcon.Trash, 1f, new(40, 0), centerTest: true, tooltip: "[HOLD TO DESTROY]", onlyIcon: true))
        {
            Capability.Destroy();
        }

        ImBrio.VerticalSeparator(24, 1);

        if(ImBrio.FontIconButton("bglifetime_rename", FontAwesomeIcon.Signature, "Rename"))
        {
            ModalManager.Instance.OpenRenameModal(Capability.Entity);
        }
    }

    public override void DrawPopup()
    {
        if(ImGui.MenuItem($"Rename {Capability.Entity.FriendlyName}###bglifetime_popup_rename"))
        {
            ImGui.CloseCurrentPopup();

            ModalManager.Instance.OpenRenameModal(Capability.Entity);
        }

        if(Capability.CanClone && ImGui.MenuItem("Clone###bglifetime_popup_clone"))
            Capability.Clone();

        if(ImGui.MenuItem("Move to Camera###bglifetime_popup_move"))
            Capability.MoveToCamera();

        if(Capability.CanDestroy)
        {
            ImGui.Separator();

            if(ImGui.BeginMenu("Destroy###bglifetime_popup_destroy"))
            {
                if(ImGui.MenuItem("Confirm Destruction###bglifetime_popup_destroy_confirm"))
                    Capability.Destroy();

                ImGui.EndMenu();
            }
        }
    }
}
