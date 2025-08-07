using Brio.Capabilities.Actor;
using Brio.Entities.Actor;
using Brio.UI.Controls.Core;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;

namespace Brio.UI.Controls.Editors;

public class ActorEditor
{
    public unsafe static void DrawSpawnMenu(ActorContainerEntity actorContainerEntity)
    {
        var hasCapability = actorContainerEntity.TryGetCapability<ActorContainerCapability>(out var capability);

        using(ImRaii.Disabled(hasCapability == false))
        {
            DrawSpawnMenu(capability!);
        }
    }

    private unsafe static void DrawSpawnMenu(ActorContainerCapability actorContainerCapability)
    {
        using var popup = ImRaii.Popup("ActorEditorDrawSpawnMenuPopup");
        if(popup.Success)
        {
            using(ImRaii.PushColor(ImGuiCol.Button, UIConstants.Transparent))
            {
                if(ImGui.Button("Spawn Actor"))
                {
                    actorContainerCapability.CreateCharacter(false, true, forceSpawnActorWithoutCompanion: true);
                }

                if(ImGui.Button("Spawn Actor with Slot"))
                {
                    actorContainerCapability.CreateCharacter(true, true);
                }

                ImGui.Separator();

                if(ImGui.Button("Spawn Prop"))
                {
                    actorContainerCapability.CreateProp(true);
                }
            }
        }
    }
}
