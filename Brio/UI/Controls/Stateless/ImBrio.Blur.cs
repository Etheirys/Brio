using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Brio.UI.Controls.Stateless;

public static partial class ImBrio
{
    private static readonly Vector4 BlurTintMultiplier = new(158 / 255f, 158 / 255f, 158 / 255f, 25 / 255f);

    private const float BlurNoiseOpacity = 0.17f;
    private const float MaxBlurStrength = 1f;

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void BlurWindow(ImGuiWindowFlags flags = ImGuiWindowFlags.None, float blurFactor = 0.10f)
    {
        var drawList = ImGui.GetWindowDrawList();

        var shouldBlur = blurFactor != 0f &&
                         ImGui.GetWindowViewport().ID == ImGui.GetMainViewport().ID &&
                         !flags.HasFlag(ImGuiWindowFlags.NoBackground);

        if(shouldBlur)
        {
            var wPos = ImGui.GetWindowPos();
            ImGuiHelpers.PrependBlurBehind(
                drawList,
                wPos,
                wPos + ImGui.GetWindowSize(),
                blurFactor * MaxBlurStrength,
                ImGui.GetStyle().WindowRounding,
                tintColor: ImGui.GetStyle().Colors[ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows)
                                        ? (int)ImGuiCol.TitleBgActive
                                        : (int)ImGuiCol.TitleBg]
                                        * BlurTintMultiplier,
                noiseOpacity: BlurNoiseOpacity * ImGui.GetStyle().Alpha);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void BlurPopup(float blurFactor = 0.10f)
    {
        var drawList = ImGui.GetWindowDrawList();

        var shouldBlur = blurFactor != 0f && ImGui.GetWindowViewport().ID == ImGui.GetMainViewport().ID;

        if(shouldBlur)
        {
            var wPos = ImGui.GetWindowPos();
            ImGuiHelpers.PrependBlurBehind(
                drawList,
                wPos,
                wPos + ImGui.GetWindowSize(),
                blurFactor * MaxBlurStrength,
                ImGui.GetStyle().WindowRounding,
                tintColor: ImGui.GetStyle().Colors[(int)ImGuiCol.PopupBg] * BlurTintMultiplier,
                noiseOpacity: BlurNoiseOpacity * ImGui.GetStyle().Alpha);
        }
    }
}
