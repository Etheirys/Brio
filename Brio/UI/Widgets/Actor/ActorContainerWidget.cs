using Brio.Capabilities.Actor;
using Brio.Entities.Actor;
using Brio.UI.Widgets.Core;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;

namespace Brio.UI.Widgets.Actor;

public class ActorContainerWidget(ActorContainerCapability capability) : Widget<ActorContainerCapability>(capability)
{
    public override string HeaderName => "Actors";
    public override WidgetFlags Flags
    {
        get
        {
            WidgetFlags flags = WidgetFlags.DrawQuickIcons;

            if(Capability.CanControlCharacters)
                flags |= WidgetFlags.DrawPopup | WidgetFlags.CanHide;

            return flags;
        }
    }

    public override void DrawQuickIcons()
    {
        using(ImRaii.Disabled(!Capability.CanControlCharacters))
        {

        }
    }

    public override void DrawPopup()
    {
        if(ImGui.BeginMenu("New...###containerwidgetpopup_new"))
        {
            if(ImGui.MenuItem("Actor###containerwidgetpopup_spawnbasic"))
            {
                Capability.CreateCharacter(false, true, forceSpawnActorWithoutCompanion: true);
            }
            if(ImGui.MenuItem("Actor with Companion###containerwidgetpopup_spawncompanion"))
            {
                Capability.CreateCharacter(true, true);
            }

            ImGui.Separator();

            if(ImGui.MenuItem("Prop###containerwidgetpopup_spawnprop"))
            {

            }

            if(ImGui.MenuItem("Furniture Item###containerwidgetpopup_spawnfur"))
            {

            }

            if(ImGui.MenuItem("World Object###containerwidgetpopup_spawnworld"))
            {

            }

            if(ImGui.MenuItem("VFX###containerwidgetpopup_spawnVFX"))
            {

            }

            ImGui.EndMenu();
        }

        if(ImGui.BeginMenu("Destroy All...###containerwidgetpopup_destroy"))
        {
            if(ImGui.BeginMenu("Actors###containerwidgetpopup_destroyActors"))
            {
                if(ImGui.MenuItem("Confirm Destruction##containerwidgetpopup_destroyallActors"))
                {
                    Capability.DestroyAll();
                }
                ImGui.EndMenu();
            }
            ImGui.EndMenu();
        }
    }
}
