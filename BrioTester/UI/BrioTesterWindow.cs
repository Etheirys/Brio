using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using ImGuiNET;
using System.Numerics;

namespace BrioTester.Plugin.UI;

public class BrioTesterWindow : Window
{
    private BrioTester _plugin { get; }

    //

    private (int, int) ver;
    private IGameObject? gameObject;

    //

    private ICallGateSubscriber<(int, int)> API_Version_IPC;

    private ICallGateSubscriber<IGameObject?> Actor_Spawn_IPC;
    private ICallGateSubscriber<Task<IGameObject?>> Actor_SpawnAsync_IPC;
    private ICallGateSubscriber<bool, bool, Task<IGameObject?>> Actor_SpawnExAsync_IPC;

    private ICallGateSubscriber<IGameObject, bool> Actor_DespawnActor_Ipc;
    private ICallGateSubscriber<IGameObject, Task<bool>> Actor_DespawnActorAsync_Ipc;

    private ICallGateSubscriber<IGameObject, Vector3, Quaternion, Vector3, bool> Actor_SetModelTransform_IPC;
    private ICallGateSubscriber<IGameObject, (Vector3?, Quaternion?, Vector3?)> Actor_GetModelTransform_IPC;

    //

    public BrioTesterWindow(IDalamudPluginInterface pluginInterface, BrioTester plugin) : base($" {BrioTester.PluginName}")
    {
        _plugin = plugin;

        API_Version_IPC = pluginInterface.GetIpcSubscriber<(int, int)>("Brio.ApiVersion");

        Actor_Spawn_IPC = pluginInterface.GetIpcSubscriber<IGameObject?>("Brio.Actor.Spawn");
        Actor_SpawnAsync_IPC = pluginInterface.GetIpcSubscriber<Task<IGameObject?>>("Brio.Actor.SpawnAsync");
        Actor_SpawnExAsync_IPC = pluginInterface.GetIpcSubscriber<bool, bool, Task<IGameObject?>>("Brio.Actor.SpawnExAsync");

        Actor_DespawnActor_Ipc = pluginInterface.GetIpcSubscriber<IGameObject, bool>("Brio.Actor.Despawn");
        Actor_DespawnActorAsync_Ipc = pluginInterface.GetIpcSubscriber<IGameObject, Task<bool>>("Brio.Actor.DespawnAsync");

        Actor_SetModelTransform_IPC = pluginInterface.GetIpcSubscriber<IGameObject, Vector3, Quaternion, Vector3, bool>("Brio.Actor.SetModelTransform");
        Actor_GetModelTransform_IPC = pluginInterface.GetIpcSubscriber<IGameObject, (Vector3?, Quaternion?, Vector3?)>("Brio.Actor.GetModelTransform");

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(520, 360),
            MaximumSize = new Vector2(1040, 720)
        };
    }

    string logText; 
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
                ver = API_Version_IPC.InvokeFunc();
            }
            ImGui.PopStyleColor();
            ImGui.SameLine();

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(86, 98, 246, 255) / 255);
            if (ImGui.Button("spawn " + gameObject?.Name, buttonSize))
            {
                gameObject = Actor_Spawn_IPC.InvokeFunc();
            }
            ImGui.PopStyleColor();
            ImGui.SameLine();

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0, 100, 255, 255) / 255);
            if (ImGui.Button("despawn " + gameObject?.Name, buttonSize))
            {
                if (gameObject is not null)
                    Actor_DespawnActor_Ipc.InvokeFunc(gameObject);
            }
            ImGui.PopStyleColor();
            ImGui.SameLine();

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(110, 84, 148, 255) / 255);
            if (ImGui.Button("move ", buttonSize))
            {
                if (gameObject is not null)
                {
                    var act = Actor_GetModelTransform_IPC.InvokeFunc(gameObject);
                    logText += $"{act} \n";
                    Actor_SetModelTransform_IPC.InvokeFunc(gameObject, act.Item1.GetValueOrDefault() + new Vector3(10, 0, 10), act.Item2.GetValueOrDefault(), act.Item3.GetValueOrDefault());
                }
            }
            ImGui.PopStyleColor();
            ImGui.SameLine();
        }
    }
}
