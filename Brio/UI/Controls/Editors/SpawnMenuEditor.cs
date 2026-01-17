using Brio.Entities.Camera;
using Brio.Game.Actor;
using Brio.Game.Camera;
using Brio.Game.World;
using Brio.UI.Controls.Core;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace Brio.UI.Controls.Editors;

public static class SpawnMenuEditor
{
    private const float MenuWidth = 220f;

    public static void DrawUnifiedSpawnMenu(
        ActorSpawnService? actorSpawnService = null,
        VirtualCameraManager? cameraManager = null,
        LightingService? lightingService = null)
    {
        using var popup = ImRaii.Popup("UnifiedSpawnMenuPopup");
        if(!popup.Success)
            return;

        var buttonSize = new Vector2(MenuWidth * ImGuiHelpers.GlobalScale, 0);

        using(ImRaii.PushColor(ImGuiCol.Button, UIConstants.Transparent))
        {
            // Actor spawn
            if(actorSpawnService != null)
            {
                ImGui.Text("Actors");
                ImGui.Separator();

                if(DrawIconButton(FontAwesomeIcon.User, "Spawn New Actor", buttonSize))
                {
                    actorSpawnService.CreateCharacter(out _, SpawnFlags.Default, true);
                    ImGui.CloseCurrentPopup();
                }

                if(DrawIconButton(FontAwesomeIcon.PlusSquare, "Spawn with Companion Slot", buttonSize))
                {
                    actorSpawnService.CreateCharacter(out _, SpawnFlags.ReserveCompanionSlot, false);
                    ImGui.CloseCurrentPopup();
                }

                if(DrawIconButton(FontAwesomeIcon.Cubes, "Spawn Prop", buttonSize))
                {
                    actorSpawnService.SpawnNewProp(out _);
                    ImGui.CloseCurrentPopup();
                }
            }

            // Camera spawn
            if(cameraManager != null)
            {
                if(actorSpawnService != null)
                    ImGui.Spacing();

                ImGui.Text("Cameras");
                ImGui.Separator();

                if(DrawIconButton(FontAwesomeIcon.Camera, "New Brio Camera", buttonSize))
                {
                    cameraManager.CreateCamera(CameraType.Game);
                    ImGui.CloseCurrentPopup();
                }

                if(DrawIconButton(FontAwesomeIcon.Video, "New Free-Cam", buttonSize))
                {
                    cameraManager.CreateCamera(CameraType.Free);
                    ImGui.CloseCurrentPopup();
                }
            }

            // Light spawn
            if(lightingService != null)
            {
                if(actorSpawnService != null || cameraManager != null)
                    ImGui.Spacing();

                ImGui.Text("Lights");
                ImGui.Separator();

                if(DrawIconButton(FontAwesomeIcon.Lightbulb, "Spawn Spot Light", buttonSize))
                {
                    lightingService.SpawnLight(LightType.SpotLight);
                    ImGui.CloseCurrentPopup();
                }

                if(DrawIconButton(FontAwesomeIcon.Circle, "Spawn Point Light", buttonSize))
                {
                    lightingService.SpawnLight(LightType.AreaLight);
                    ImGui.CloseCurrentPopup();
                }

                if(DrawIconButton(FontAwesomeIcon.Square, "Spawn Flat Light", buttonSize))
                {
                    lightingService.SpawnLight(LightType.FlatLight);
                    ImGui.CloseCurrentPopup();
                }
            }
        }
    }

    private static bool DrawIconButton(FontAwesomeIcon icon, string text, Vector2 size)
    {
        var cursorPos = ImGui.GetCursorPos();
        bool clicked = ImGui.Button($"##{text}", size);

        ImGui.SetCursorPos(cursorPos + new Vector2(5 * ImGuiHelpers.GlobalScale, ImGui.GetStyle().FramePadding.Y));

        using(ImRaii.PushFont(UiBuilder.IconFont))
        {
            ImGui.Text(icon.ToIconString());
        }

        ImGui.SameLine();
        ImGui.SetCursorPosY(cursorPos.Y + ImGui.GetStyle().FramePadding.Y);
        ImGui.Text(text);

        return clicked;
    }
}

