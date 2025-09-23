using Dalamud.Bindings.ImGui;
using System;
using System.Runtime.CompilerServices;

namespace Brio.UI.Controls.Stateless;
public static partial class ImBrio
{
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static float GetRemainingWidth()
    {
        return ImGui.GetContentRegionAvail().X;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static float GetRemainingHeight()
    {
        return ImGui.GetContentRegionAvail().Y;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static float GetLineHeight()
    {
        return ImGui.GetTextLineHeight() + (ImGui.GetStyle().FramePadding.Y * 2);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void RightAlign(float width, int numItems)
    {
        RightAlign((width * numItems) + (ImGui.GetStyle().ItemSpacing.X * (numItems - 1)));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void RightAlign(float width)
    {
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (GetRemainingWidth() - width));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void CenterNextElementWithPadding(float padding)
    {
        var elementWidth = GetRemainingWidth() - padding;

        float windowWidth = ImGui.GetContentRegionAvail().X;
        float offset = MathF.Max(0, (windowWidth - elementWidth) * 0.5f);
      
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);
        ImGui.SetNextItemWidth(elementWidth);
    }

}
