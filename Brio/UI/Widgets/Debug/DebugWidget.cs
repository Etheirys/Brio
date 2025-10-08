using Brio.Capabilities.Debug;
using Brio.Game.World;
using Brio.IPC;
using Brio.UI.Controls.Stateless;
using Brio.UI.Widgets.Core;
using Brio.UI.Widgets.World;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;

namespace Brio.UI.Widgets.Debug;

public class DebugWidget(DebugCapability capability, IClientState _clientState) : Widget<DebugCapability>(capability)
{
    public override string HeaderName => "Debug";

    public override WidgetFlags Flags => WidgetFlags.DrawBody;

    public override void DrawBody()
    {
        using(var bar = ImRaii.TabBar("DebugTabBar"))
        {
            if(bar.Success)
            {
                using(var item = ImRaii.TabItem("GPose"))
                {
                    if(item.Success)
                        DrawGPose();
                }

                using(var item = ImRaii.TabItem("Addresses"))
                {
                    if(item.Success)
                        DrawAddresses();
                }

                using(var item = ImRaii.TabItem("Misc"))
                {
                    if(item.Success)
                        DrawMisc();
                }
            }
        }
    }

    private void DrawGPose()
    {
        bool fakeGPose = Capability.FakeGPose;
        if(ImGui.Checkbox("Fake GPose", ref fakeGPose))
        {
            Capability.FakeGPose = fakeGPose;
        }

        if(ImGui.Button("Enter GPose"))
        {
            Capability.EnterGPose();
        }

        ImGui.SameLine();

        if(ImGui.Button("Exit GPose"))
        {
            Capability.ExitGPose();
        }

        ImGui.Text($"IsPosing: {Capability.IsPosing}");
    }

    private unsafe void DrawAddresses()
    {
        DynamisIPC.Instance?.DrawPointer((nint)BrioEnvManager.Instance());

        foreach(var (desc, addr) in Capability.GetInterestingAddresses())
        {
            string addrStr = addr.ToString("X");

            ImGui.SetNextItemWidth(150);
            ImBrio.CenterNextElementWithPadding(10);
            ImGui.InputText(desc, ref addrStr, 16, ImGuiInputTextFlags.ReadOnly);

            DynamisIPC.Instance?.DrawPointer(addr);
        }
    }
    private void DrawMisc()
    {
        var io = ImGui.GetIO();

        ImGui.Text($"MapId - {_clientState.MapId}");
        ImGui.Text($"TerritoryType - {_clientState.TerritoryType}");
        ImGui.Text($"CurrentWorld - {_clientState.LocalPlayer?.CurrentWorld.Value.Name}");
        ImGui.Text($"HomeWorld - {_clientState.LocalPlayer?.HomeWorld.Value.Name}");

        ImGui.Text(io.Framerate.ToString("F2") + " FPS");
    }
}
