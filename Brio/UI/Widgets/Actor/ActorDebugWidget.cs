using Brio.Capabilities.Actor;
using Brio.Game.Actor.Extensions;
using Brio.UI.Widgets.Core;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;

namespace Brio.UI.Widgets.Actor;

public class ActorDebugWidget(ActorDebugCapability capability) : Widget<ActorDebugCapability>(capability)
{
    public override string HeaderName => "Debug";

    public override WidgetFlags Flags => Capability.IsDebug ? WidgetFlags.DrawBody : WidgetFlags.None;

    public unsafe override void DrawBody()
    {
        using(var tabBar = ImRaii.TabBar("###debug_tabs"))
        {
            if(tabBar.Success)
            {
                using(var infoTab = ImRaii.TabItem("Info"))
                {
                    if(infoTab.Success)
                    {
                        string addr = Capability.GameObject.Address.ToString("X");
                        ImGui.SetNextItemWidth(-ImGui.CalcTextSize("Address").X);
                        ImGui.InputText("Address", ref addr, 256, ImGuiInputTextFlags.ReadOnly);


                        var charaBase = Capability.Character.GetCharacterBase();
                        if(charaBase != null)
                        {
                            addr = ((nint)charaBase).ToString("X");
                            ImGui.SetNextItemWidth(-ImGui.CalcTextSize("DrawObject").X);
                            ImGui.InputText("DrawObject", ref addr, 256, ImGuiInputTextFlags.ReadOnly);

                            var skele = charaBase->CharacterBase.Skeleton;
                            addr = ((nint)skele).ToString("X");
                            ImGui.SetNextItemWidth(-ImGui.CalcTextSize("Skeleton").X);
                            ImGui.InputText("Skeleton", ref addr, 256, ImGuiInputTextFlags.ReadOnly);


                            var shaders = Capability.Character.GetShaderParams();
                            addr = ((nint)shaders).ToString("X");
                            ImGui.SetNextItemWidth(-ImGui.CalcTextSize("Shaders").X);
                            ImGui.InputText("Shaders", ref addr, 256, ImGuiInputTextFlags.ReadOnly);
                        }
                    }
                }

                using(var infoTab = ImRaii.TabItem("Skeleton"))
                {
                    if(infoTab.Success)
                    {
                        if(ImGui.CollapsingHeader("Stacks", ImGuiTreeNodeFlags.DefaultOpen))
                        {
                            var stacks = Capability.SkeletonStacks;
                            foreach(var stack in stacks)
                            {
                                ImGui.Text($"{stack.Key}: {stack.Value}");
                            }
                        }
                    }
                }

                using(var infoTab = ImRaii.TabItem("GameObject"))
                {
                    if(infoTab.Success)
                    {
                        Dalamud.Utility.Util.ShowGameObjectStruct(Capability.GameObject, true);
                    }
                }

            }
        }
    }
}
