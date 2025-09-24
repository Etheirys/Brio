using Brio.Capabilities.Actor;
using Brio.Entities.Actor;
using Brio.UI.Controls.Core;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

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
        using var popup = ImRaii.Popup("ActorEditorDrawSpawnMenuPopup"u8);
        if(popup.Success)
        {
            using(ImRaii.PushColor(ImGuiCol.Button, UIConstants.Transparent))
            {
                if(ImGui.Button("Spawn Actor"u8, new(155 * ImGuiHelpers.GlobalScale, 0)))
                {
                    actorContainerCapability.CreateCharacter(false, true, forceSpawnActorWithoutCompanion: true);
                }

                if(ImGui.Button("Spawn Actor with Slot"u8, new(155 * ImGuiHelpers.GlobalScale, 0)))
                {
                    actorContainerCapability.CreateCharacter(true, true);
                }

                ImGui.Separator();

                if(ImGui.Button("Spawn Prop"u8, new(155 * ImGuiHelpers.GlobalScale, 0)))
                {
                    actorContainerCapability.CreateProp(true);
                }
            }
        }
    }
}
