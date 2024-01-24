using Dalamud.Interface.Internal;
using ImGuiNET;
using System;
using System.Numerics;


namespace Brio.UI.Controls.Stateless;

internal static partial class ImBrio
{
    public static void ImageFit(IDalamudTextureWrap texture)
    {
        ImageFit(texture, ImGui.GetContentRegionAvail());
    }

    public static void ImageFit(IDalamudTextureWrap texture, Vector2 size)
    {
        float fitWidth = size.X;
        float fitHeight = size.X;
        float indent = 0;

        if(texture.Width < texture.Height)
        {
            fitWidth = ((float)texture.Width / (float)texture.Height) * ImGui.GetContentRegionAvail().X;
            indent = (ImGui.GetContentRegionAvail().X - fitWidth) / 2;
            ImGui.Indent(indent);
        }

        else if(texture.Height < texture.Width)
        {
            fitHeight = ((float)texture.Height / (float)texture.Width) * ImGui.GetContentRegionAvail().X;
        }

        ImGui.Image(texture.ImGuiHandle, new(fitWidth, fitHeight));

        if(indent != 0)
        {
            ImGui.Unindent(indent);
        }
    }

    public static void ImageRotated(IDalamudTextureWrap texture, float angle)
    {
        Vector2 center = ImGui.GetCursorScreenPos() + (texture.Size / 2);
        ImBrio.ImageRotated(texture.ImGuiHandle, center, texture.Size, angle);
    }

    public static void ImageRotated(nint tex_id, Vector2 center, Vector2 size, float angle)
    {
        ImDrawListPtr draw_list = ImGui.GetWindowDrawList();

        float cos_a = (float)Math.Cos(angle);
        float sin_a = (float)Math.Sin(angle);

        var pos1 = center + ImRotate(new Vector2(-size.X * 0.5f, -size.Y * 0.5f), cos_a, sin_a);
        var pos2 = center + ImRotate(new Vector2(+size.X * 0.5f, -size.Y * 0.5f), cos_a, sin_a);
        var pos3 = center + ImRotate(new Vector2(+size.X * 0.5f, +size.Y * 0.5f), cos_a, sin_a);
        var pos4 = center + ImRotate(new Vector2(-size.X * 0.5f, +size.Y * 0.5f), cos_a, sin_a);

        draw_list.AddImageQuad(tex_id, pos1, pos2, pos3, pos4, new Vector2(0.0f, 0.0f), new Vector2(1.0f, 0.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 1.0f), 0xFFFFFFFF);
    }

    static Vector2 ImRotate(Vector2 v, float cos_a, float sin_a)
    {
        return new Vector2(v.X * cos_a - v.Y * sin_a, v.X * sin_a + v.Y * cos_a);
    }
}
