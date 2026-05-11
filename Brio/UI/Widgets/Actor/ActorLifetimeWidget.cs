using Brio.Capabilities.Actor;
using Brio.UI.Controls;
using Brio.UI.Controls.Editors;
using Brio.UI.Controls.Stateless;
using Brio.UI.Widgets.Core;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;

namespace Brio.UI.Widgets.Actor;

public class ActorLifetimeWidget(ActorLifetimeCapability capability) : Widget<ActorLifetimeCapability>(capability)
{
    public override string HeaderName => "Lifetime";

    public override WidgetFlags Flags => WidgetFlags.DrawPopup | WidgetFlags.DrawQuickIcons;

    public override void DrawQuickIcons()
    {
        if(ImBrio.FontIconButton("lifetimewidget_spawnnew", FontAwesomeIcon.Plus, "Reload New"))
        {
            SpawnMenu.OpenUnifiedSpawnMenu();
        }

        ImGui.SameLine();

        if(ImBrio.FontIconButton("lifetimewidget_clone", FontAwesomeIcon.Clone, "Clone", Capability.CanClone))
        {
            Capability.Clone(false);
        }

        ImGui.SameLine();

        if(ImBrio.FontIconButton("lifetimewidget_target", FontAwesomeIcon.Bullseye, "Target"))
        {
            Capability.Target();
        }

        ImGui.SameLine();

        if(ImBrio.FontIconButton("lifetimewidget_destroy", FontAwesomeIcon.Trash, "Destroy", Capability.CanDestroy))
        {
            Capability.Destroy();
        }

        ImGui.SameLine();

        if(ImBrio.FontIconButton("lifetimewidget_rename", FontAwesomeIcon.Signature, "Rename"))
        {
            RenameActorModal.Open(Capability.Actor);
        }
    }

    public override void DrawPopup()
    {
        if(Capability.CanClone)
        {
            if(ImGui.MenuItem("Clone###actorlifetime_clone"))
            {
                Capability.Clone(true);
            }
        }

        if(Capability.CanDestroy)
        {
            if(ImGui.BeginMenu("Destroy###actorlifetime_destroy"))
            {
                if(ImGui.MenuItem("Confirm Destruction###actorlifetime_destroy_confirm"))
                {
                    Capability.Destroy();
                }

                ImGui.EndMenu();
            }
        }

        if(ImGui.MenuItem($"Rename {Capability.Actor.FriendlyName}###actorlifetime_rename"))
        {
            ImGui.CloseCurrentPopup();

            RenameActorModal.Open(Capability.Actor);
        }

        if(ImGui.MenuItem("Target###actorlifetime_target"))
        {
            Capability.Target();
        }

        if(ImGui.MenuItem("Move to Camera###actorlifetime_move_to_camera"))
        {
            Capability.MoveToCamera();
        }
    }
}
