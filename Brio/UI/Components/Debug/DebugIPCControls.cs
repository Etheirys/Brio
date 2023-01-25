using Brio.IPC;
using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using ImGuiNET;
using System.Threading.Tasks;

namespace Brio.UI.Components.Debug;
public static class DebugIPCControls
{
    public static void Draw()
    {
        if(ImGui.Button("Get API Version"))
        {
            var func = Dalamud.PluginInterface.GetIpcSubscriber<(int, int)>(BrioIPCService.ApiVersionIpcName);
            var result = func.InvokeFunc();
            var text = $"API Version: ({result.Item1}, {result.Item2})";
            Dalamud.ChatGui.Print(text);
            Dalamud.ToastGui.ShowNormal(text);
        }

        if(ImGui.Button("Spawn Actor"))
        {
            var func = Dalamud.PluginInterface.GetIpcSubscriber<GameObject?>(BrioIPCService.SpawnActorIpcName);
            func.InvokeFunc();
        }

        ImGui.SameLine();

        if(ImGui.Button("Spawn Actor Async"))
        {
            var func = Dalamud.PluginInterface.GetIpcSubscriber<Task<GameObject?>>(BrioIPCService.SpawnActorAsyncIpcName);
            Task.Run(async () =>
            {
                var result = await func.InvokeFunc();
                Dalamud.ChatGui.Print($"Async Result: {result?.ObjectIndex}");
            });
        }

        GameObject? gPoseTarget = null;
        unsafe
        {
            var rawTarget = TargetSystem.Instance()->GPoseTarget;
            if(rawTarget != null)
                gPoseTarget = Dalamud.ObjectTable.CreateObjectReference((nint)rawTarget);
        }

        if(gPoseTarget == null) ImGui.BeginDisabled();
        if(ImGui.Button("Despawn Actor"))
        {
            var func = Dalamud.PluginInterface.GetIpcSubscriber<GameObject?, bool>(BrioIPCService.DespawnActorIpcName);
            func.InvokeFunc(gPoseTarget);
        }

        ImGui.SameLine();

        if(ImGui.Button("Despawn Actor Async"))
        {
            var func = Dalamud.PluginInterface.GetIpcSubscriber<GameObject?, Task<bool>>(BrioIPCService.DespawnActorAsyncIpcName);
            Task.Run(async () =>
            {
                var result = await func.InvokeFunc(gPoseTarget);
                Dalamud.ChatGui.Print($"Async Result: {result}");
            });
        }
        if(gPoseTarget == null) ImGui.EndDisabled();

    }
}
