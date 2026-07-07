using Brio.Capabilities.Actor;
using Brio.Game.Actor.Extensions;
using Brio.Game.VFX.Intertop;
using Brio.IPC;
using Brio.UI.Widgets.Core;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;

namespace Brio.UI.Widgets.Actor;

public class ActorDebugWidget(ActorDebugCapability capability) : Widget<ActorDebugCapability>(capability)
{
    public override string HeaderName => "Debug";

    public override WidgetFlags Flags => Capability.IsDebug ? WidgetFlags.DrawBody : WidgetFlags.None;

    // TODO: Store this properly in a list or whatever so it can be cleaned up
    private unsafe static VfxData* _spawnedGoopInstance;

    string path = "vfx/common/eff/c0101_stlp_mim_gre_c0r1.avfx";

    public unsafe override void DrawBody()
    {
        using var tabBar = ImRaii.TabBar("###debug_tabs");
        if(tabBar.Success)
        {
            using(var infoTab = ImRaii.TabItem("Info"))
            {
                if(infoTab.Success)
                {
                    if(DynamisService.Instance != null)
                    {
                        ImGui.Text("GameObject ");
                        ImGui.SameLine();
                        DynamisService.Instance.DrawPointer(Capability.GameObject.Address);
                    }
                    else
                    {
                        string addr = Capability.GameObject.Address.ToString("X");
                        ImGui.SetNextItemWidth(-ImGui.CalcTextSize("Address").X);
                        ImGui.InputText("Address", ref addr, 256, ImGuiInputTextFlags.ReadOnly);
                    }

                    var charaBase = Capability.Character.GetCharacterBase();
                    if(charaBase != null)
                    {
                        if(DynamisService.Instance != null)
                        {
                            ImGui.Text("Character BaseObject ");
                            ImGui.SameLine();
                            DynamisService.Instance.DrawPointer((nint)charaBase);
                        }
                        else
                        {
                            var addr = ((nint)charaBase).ToString("X");
                            ImGui.SetNextItemWidth(-ImGui.CalcTextSize("DrawObject").X - 10);
                            ImGui.InputText("DrawObject", ref addr, 256, ImGuiInputTextFlags.ReadOnly);
                        }

                        var skele = charaBase->CharacterBase.Skeleton;
                        if(DynamisService.Instance != null)
                        {
                            ImGui.Text("Skeleton ");
                            ImGui.SameLine();
                            DynamisService.Instance.DrawPointer((nint)skele);
                        }
                        else
                        {
                            var addr = ((nint)skele).ToString("X");
                            ImGui.SetNextItemWidth(-ImGui.CalcTextSize("Skeleton").X - 10);
                            ImGui.InputText("Skeleton", ref addr, 256, ImGuiInputTextFlags.ReadOnly);
                        }

                        var shaders = Capability.Character.GetShaderParams();
                        if(DynamisService.Instance != null)
                        {
                            ImGui.Text("Character Shader ");
                            ImGui.SameLine();
                            DynamisService.Instance.DrawPointer((nint)shaders);
                        }
                        else
                        {
                            var addr = ((nint)shaders).ToString("X");
                            ImGui.SetNextItemWidth(-ImGui.CalcTextSize("Shaders").X - 10);
                            ImGui.InputText("Shaders", ref addr, 256, ImGuiInputTextFlags.ReadOnly);
                        }
                    }
                }
            }

            using(var infoTab = ImRaii.TabItem("Skeleton"))
            {
                if(infoTab.Success)
                {
                    if(ImGui.Button("Refresh Skeleton Cache"))
                    {
                        Capability.SkeletonService.RefreshSkeletonCache();
                    }

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

            using(var vfxTab = ImRaii.TabItem("Goop Demo"))
            {
                if(vfxTab.Success)
                {

                    ImGui.InputText("Path", ref path);
                    if(ImGui.Button("Create Actor VFX"))
                    {
                        // TODO: Store this properly in a list or whatever so it can be cleaned up
                        _spawnedGoopInstance = Capability.VFXService.CreateActorVFX(path, Capability.GameObject);

                    }

                    if(DynamisService.Instance != null)
                    {
                        ImGui.Text("VfxData: ");
                        ImGui.SameLine();
                        DynamisService.Instance.DrawPointer((nint)_spawnedGoopInstance);
                    }

                    if(ImGui.Button("Destroy Actor VFX"))
                    {
                        Capability.VFXService.DestroyVFX(_spawnedGoopInstance);
                        _spawnedGoopInstance = null;
                    }
                }
            }
        }
    }
}
