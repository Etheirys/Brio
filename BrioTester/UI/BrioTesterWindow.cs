using Brio;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using ImGuiNET;
using System.Numerics;

namespace BrioTester.Plugin.UI;

public class BrioTesterWindow : Window
{
    private (int, int) ver;
    private IGameObject? gameObject;

    private readonly string testURI = @""; // <YourPathHere>

    //

    public BrioTesterWindow(IDalamudPluginInterface pluginInterface) : base($" {BrioTester.PluginName}")
    {
        BrioAPI.InitBrioAPI(pluginInterface);

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(520, 360),
            MaximumSize = new Vector2(1040, 720)
        };
    }

    private string logText = "";
    public override void Draw()
    {
        var segmentSize = ImGui.GetWindowSize().X / 4.5f;
        var buttonSize = new Vector2(segmentSize, ImGui.GetTextLineHeight() * 1.8f);

        using (var textGroup = ImRaii.Group())
        {
            if (textGroup.Success)
            {
                ImGui.PushTextWrapPos(segmentSize * 4);
                ImGui.TextWrapped(logText);
                ImGui.PopTextWrapPos();
            }
        }

        using var buttonGroup = ImRaii.Group();
        if (buttonGroup.Success)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(255, 0, 0, 255) / 255);
            if (ImGui.Button("brio v." + ver, buttonSize))
            {
                if (BrioAPI.IsVersionCompatible())
                {
                    ver = BrioAPI.GetVersion();

                    if (gameObject is not null && string.IsNullOrWhiteSpace(testURI) == false)
                        BrioAPI.SetActorPoseFromFilePath(gameObject, testURI);
                }
            }
            ImGui.PopStyleColor();
            ImGui.SameLine();

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(86, 98, 246, 255) / 255);
            if (ImGui.Button("spawn " + gameObject?.Name, buttonSize))
            {
                gameObject = BrioAPI.SpawnActor();
            }
            ImGui.PopStyleColor();
            ImGui.SameLine();

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0, 100, 255, 255) / 255);
            if (ImGui.Button("despawn " + gameObject?.Name, buttonSize))
            {
                if (gameObject is not null && BrioAPI.ActorExists(gameObject))
                {
                    logText += $"Despawned Actor \n";
                    BrioAPI.DespawnActor(gameObject);
                }
            }
            ImGui.PopStyleColor();
            ImGui.SameLine();

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(110, 84, 148, 255) / 255);
            if (ImGui.Button("move ", buttonSize))
            {
                if (gameObject is not null)
                {
                    var act = BrioAPI.GetActorModelTransform(gameObject);
                    logText += $"{act} \n";

                    //BrioAPI.SetActorModelTransform(gameObject, act.Position.GetValueOrDefault() + new Vector3(10, 0, 10), act.Rotation.GetValueOrDefault(), act.Scale.GetValueOrDefault(), false);
                    BrioAPI.SetActorModelTransform(gameObject, new Vector3(10, 0, 10), null, null, true);
                    //BrioAPI.SetActorPosition(gameObject, new Vector3(10, 0, 10));
                }
            }
            ImGui.PopStyleColor();
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(110, 84, 148, 255) / 255);
            if (ImGui.Button("reset Transform", buttonSize))
            {
                if (gameObject is not null)
                {
                    BrioAPI.ResetActorModelTransform(gameObject);
                }
            }
            ImGui.PopStyleColor();

            ImGui.SameLine();

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(110, 84, 148, 255) / 255);
            if (ImGui.Button("reset Pose", buttonSize))
            {
                if (gameObject is not null)
                {
                    BrioAPI.ResetActorPose(gameObject);
                }
            }
            ImGui.PopStyleColor();

            ImGui.SameLine();

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0, 100, 255, 255) / 255);
            if (ImGui.Button("get all " + gameObject?.Name, buttonSize))
            {
                var (HasAtLeastOne, Actors) = BrioAPI.GetAllActiveActors();
                if (HasAtLeastOne)
                {
                    foreach (var actor in Actors)
                    {
                        logText += $"Actor {actor.Name} \n";
                    }
                }
            }
            ImGui.PopStyleColor();

        }
    }
}
