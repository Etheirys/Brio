using Brio.Utils;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface;
using ImGuiNET;
using System.Numerics;

using DalamudGameObject = Dalamud.Game.ClientState.Objects.Types.GameObject;
using GameObject = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;

namespace Brio.UI.Components;

public static class GPoseActorPropertyControls
{
    public unsafe static void Draw(DalamudGameObject gameObject)
    {
        string name = gameObject.Name.ToString();
        string originalName = name;

        ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize("Name").X);

        if (ImGui.InputText("Name", ref name, 63))
        {
            if (name != originalName)
            {
                var go = (GameObject*)gameObject.Address;
                (*go).SetName(name);
            }
        }

        ImGui.PopItemWidth();

        if(ImGui.Button("Force NPC Face Hack"))
        {
            GameObject* raw= (GameObject*)gameObject.Address;
            if(raw->ObjectKind == (byte) ObjectKind.Player)
            {
                raw->DisableDraw();
                raw->ObjectKind = (byte)ObjectKind.BattleNpc;
                raw->EnableDraw();
                raw->ObjectKind = (byte)ObjectKind.Player;
            }
        }

    }
}
