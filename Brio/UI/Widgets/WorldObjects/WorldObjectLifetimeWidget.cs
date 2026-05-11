using Brio.Capabilities.WorldObjects;
using Brio.UI.Controls;
using Brio.UI.Controls.Editors;
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
        if(ImBrio.FontIconButton("bglifetime_spawnnew", FontAwesomeIcon.Plus, "New..."))
            SpawnMenu.OpenUnifiedSpawnMenu();

        ImGui.SameLine();

        if(ImBrio.FontIconButton("bglifetime_movetocamera", FontAwesomeIcon.CameraRotate, "Move to Camera"))
            Capability.MoveToCamera();

        ImGui.SameLine();

        if(ImBrio.FontIconButton("bglifetime_clone", FontAwesomeIcon.Clone, "Clone", Capability.CanClone))
            Capability.Clone();

        ImGui.SameLine();

        if(ImBrio.FontIconButton("bglifetime_destroy", FontAwesomeIcon.Trash, "Destroy", Capability.CanDestroy))
            Capability.Destroy();

        ImGui.SameLine();

        if(ImBrio.FontIconButton("bglifetime_rename", FontAwesomeIcon.Signature, "Rename"))
            RenameActorModal.Open(Capability.Entity);
    }

    public override void DrawPopup()
    {
        if(ImGui.MenuItem("Move to Camera###bglifetime_popup_move"))
            Capability.MoveToCamera();

        if(Capability.CanClone && ImGui.MenuItem("Clone###bglifetime_popup_clone"))
            Capability.Clone();

        if(Capability.CanDestroy && ImGui.MenuItem("Destroy###bglifetime_popup_destroy"))
            Capability.Destroy();
    }
}
