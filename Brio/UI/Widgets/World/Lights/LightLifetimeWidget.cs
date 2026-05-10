using Brio.Capabilities.World;
using Brio.Game.Actor;
using Brio.Game.Camera;
using Brio.Game.World;
using Brio.UI.Controls;
using Brio.UI.Controls.Editors;
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
        if(ImBrio.FontIconButton("lifetimewidget_spawnnew", FontAwesomeIcon.Plus, "Spawn New"))
        {
            SpawnMenu.OpenUnifiedSpawnMenu();
        }

        ImGui.SameLine();

        if(ImBrio.FontIconButton("lifetimewidget_move_to_camera", FontAwesomeIcon.Thumbtack, "Move to Camera"))
        {
            Capability.MoveToCamera();
        }

        ImGui.SameLine();

        if(ImBrio.FontIconButton("lifetimewidget_clone", FontAwesomeIcon.Clone, "Clone Light", Capability.CanClone))
        {
            Capability.Clone();
        }

        ImGui.SameLine();

        if(ImBrio.FontIconButton("lifetimewidget_destroy", FontAwesomeIcon.Trash, "Destroy Light", Capability.CanDestroy))
        {
            Capability.Destroy();
        }

        ImGui.SameLine();

        if(ImBrio.FontIconButton("lifetimewidget_rename", FontAwesomeIcon.Signature, "Rename Light"))
        {
            RenameActorModal.Open(Capability.Entity);
        }

        ImGui.SameLine();

        if(ImBrio.FontIconButtonRight($"lifetimewidget_openAdvaned", FontAwesomeIcon.SquareArrowUpRight, 1, Capability.IsLightWindowOpen ? "Close Light Window" : "Open Light Window"))
        {
            Capability.ToggleLightWindow();
        }
    }

    public override void DrawPopup()
    {
        if(ImGui.MenuItem("Move to Camera###actorlifetime_move_to_camera"))
        {
            Capability.MoveToCamera();
        }

        if(Capability.CanClone)
        {
            if(ImGui.MenuItem("Clone###actorlifetime_clone"))
            {
                Capability.Clone();
            }
        }

        if(Capability.CanDestroy)
        {
            if(ImGui.MenuItem("Destroy###actorlifetime_destroy"))
            {
                Capability.Destroy();
            }
        }

        if(ImGui.MenuItem($"Rename {Capability.Entity.FriendlyName}###actorlifetime_rename"))
        {
            ImGui.CloseCurrentPopup();

            RenameActorModal.Open(Capability.Entity);
        }

        if(ImGui.MenuItem("Open Light Window###actorlifetime_lightwindow"))
        {
            Capability.OpenLightWindow();
        }
    }
}
