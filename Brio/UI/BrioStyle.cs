using Dalamud.Bindings.ImGui;
using System.Numerics;

namespace Brio.UI;
public static class BrioStyle
{
    public static bool EnableStyle { get; set; }
    public static bool EnableColor => Config.ConfigurationService.Instance.Configuration.Appearance.EnableBrioColor;
    //public static bool EnableScale => Config.ConfigurationService.Instance.Configuration.Appearance.EnableBrioScale;

    static bool _hasPushed = false;
    static bool _hasPushedColor = false;
    //static bool _hasPushedScale = false;
    //static float _lastGlobalScale;

    public static void PushStyle()
    {
        if(EnableStyle == false)
        {
            return;
        }
        _hasPushed = true;

        //var imIO = ImGui.GetIO();
        //if(EnableScale)
        //{
        //    _lastGlobalScale = imIO.FontGlobalScale;
        //    imIO.FontGlobalScale = 1f;
        //    _hasPushedScale = true;
        //}

        if(EnableColor)
        {
            _hasPushedColor = true;

            PushStyleColor(ImGuiCol.Text, new Vector4(255, 255, 255, 255));
            PushStyleColor(ImGuiCol.TextDisabled, new Vector4(128, 128, 128, 255));

            PushStyleColor(ImGuiCol.WindowBg, new Vector4(25, 25, 25, 248));
            PushStyleColor(ImGuiCol.ChildBg, new Vector4(25, 25, 25, 66));
            PushStyleColor(ImGuiCol.PopupBg, new Vector4(25, 25, 25, 248));

            PushStyleColor(ImGuiCol.Border, new Vector4(44, 44, 44, 255));
            PushStyleColor(ImGuiCol.BorderShadow, new Vector4(0, 0, 0, 128));

            PushStyleColor(ImGuiCol.FrameBg, new Vector4(36, 36, 36, 255));
            PushStyleColor(ImGuiCol.FrameBgHovered, new Vector4(57, 57, 57, 255));
            PushStyleColor(ImGuiCol.FrameBgActive, new Vector4(33, 33, 3, 255));

            PushStyleColor(ImGuiCol.TitleBg, new Vector4(27, 27, 27, 232));
            PushStyleColor(ImGuiCol.TitleBgActive, new Vector4(33, 33, 33, 255));
            PushStyleColor(ImGuiCol.TitleBgCollapsed, new Vector4(30, 30, 30, 255));

            PushStyleColor(ImGuiCol.MenuBarBg, new Vector4(36, 36, 36, 255));
            PushStyleColor(ImGuiCol.ScrollbarBg, new Vector4(0, 0, 0, 0));
            PushStyleColor(ImGuiCol.ScrollbarGrab, new Vector4(62, 62, 62, 255));
            PushStyleColor(ImGuiCol.ScrollbarGrabHovered, new Vector4(70, 70, 70, 255));
            PushStyleColor(ImGuiCol.ScrollbarGrabActive, new Vector4(70, 70, 70, 255));

            PushStyleColor(ImGuiCol.CheckMark, new Vector4(98, 75, 224, 255));

            PushStyleColor(ImGuiCol.SliderGrab, new Vector4(101, 101, 101, 255));
            PushStyleColor(ImGuiCol.SliderGrabActive, new Vector4(123, 123, 123, 255));

            PushStyleColor(ImGuiCol.Button, new Vector4(255, 255, 255, 31));
            PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(74, 56, 170, 255));
            PushStyleColor(ImGuiCol.ButtonActive, new Vector4(54, 42, 122, 255));

            PushStyleColor(ImGuiCol.Header, new Vector4(0, 0, 0, 60));
            PushStyleColor(ImGuiCol.HeaderHovered, new Vector4(0, 0, 0, 90));
            PushStyleColor(ImGuiCol.HeaderActive, new Vector4(0, 0, 0, 120));

            PushStyleColor(ImGuiCol.Separator, new Vector4(75, 75, 75, 121));
            PushStyleColor(ImGuiCol.SeparatorHovered, new Vector4(37, 25, 98, 255));
            PushStyleColor(ImGuiCol.SeparatorActive, new Vector4(98, 75, 224, 255));

            PushStyleColor(ImGuiCol.ResizeGrip, new Vector4(0, 0, 0, 0));
            PushStyleColor(ImGuiCol.ResizeGripHovered, new Vector4(0, 0, 0, 0));
            PushStyleColor(ImGuiCol.ResizeGripActive, new Vector4(98, 75, 224, 255));

            PushStyleColor(ImGuiCol.Tab, new Vector4(41, 41, 41, 255));
            PushStyleColor(ImGuiCol.TabHovered, new Vector4(42, 29, 113, 255));
            PushStyleColor(ImGuiCol.TabActive, new Vector4(98, 75, 224, 255));
            PushStyleColor(ImGuiCol.TabUnfocused, new Vector4(41, 39, 41, 255));
            PushStyleColor(ImGuiCol.TabUnfocusedActive, new Vector4(73, 48, 205, 255));

            PushStyleColor(ImGuiCol.DockingPreview, new Vector4(91, 70, 208, 105));
            PushStyleColor(ImGuiCol.DockingEmptyBg, new Vector4(51, 51, 51, 255));

            PushStyleColor(ImGuiCol.PlotLines, new Vector4(156, 156, 156, 255));

            PushStyleColor(ImGuiCol.TableHeaderBg, new Vector4(48, 48, 48, 255));
            PushStyleColor(ImGuiCol.TableBorderStrong, new Vector4(79, 79, 89, 255));
            PushStyleColor(ImGuiCol.TableBorderLight, new Vector4(59, 59, 64, 255));
            PushStyleColor(ImGuiCol.TableRowBg, new Vector4(0, 0, 0, 0));
            PushStyleColor(ImGuiCol.TableRowBgAlt, new Vector4(255, 255, 255, 15));

            PushStyleColor(ImGuiCol.TextSelectedBg, new Vector4(98, 75, 224, 255));
            PushStyleColor(ImGuiCol.DragDropTarget, new Vector4(98, 75, 224, 255));

            PushStyleColor(ImGuiCol.NavHighlight, new Vector4(98, 75, 224, 179));
            PushStyleColor(ImGuiCol.NavWindowingDimBg, new Vector4(204, 204, 204, 51));
            PushStyleColor(ImGuiCol.NavWindowingHighlight, new Vector4(204, 204, 204, 89));
        }

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(6, 6));
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(4, 3));
        ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(4, 4));
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(4, 4));
        ImGui.PushStyleVar(ImGuiStyleVar.ItemInnerSpacing, new Vector2(4, 4));

        ImGui.PushStyleVar(ImGuiStyleVar.IndentSpacing, 21.0f);
        ImGui.PushStyleVar(ImGuiStyleVar.ScrollbarSize, 5.0f);
        ImGui.PushStyleVar(ImGuiStyleVar.GrabMinSize, 20.0f);

        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 1f);
        ImGui.PushStyleVar(ImGuiStyleVar.ChildBorderSize, 1f);
        ImGui.PushStyleVar(ImGuiStyleVar.PopupBorderSize, 1f);
        ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 0f);

        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 7f);
        ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 4f);
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 4f);
        ImGui.PushStyleVar(ImGuiStyleVar.PopupRounding, 4f);
        ImGui.PushStyleVar(ImGuiStyleVar.ScrollbarRounding, 4f);
        ImGui.PushStyleVar(ImGuiStyleVar.GrabRounding, 4f);
        ImGui.PushStyleVar(ImGuiStyleVar.TabRounding, 4f);
    }

    static void PushStyleColor(ImGuiCol imGuiCol, Vector4 colorVector)
    {
        uint r = (uint)(colorVector.X) & 0xFF;
        uint g = (uint)(colorVector.Y) & 0xFF;
        uint b = (uint)(colorVector.Z) & 0xFF;
        uint a = (uint)(colorVector.W) & 0xFF;

        ImGui.PushStyleColor(imGuiCol, (a << 24) | (b << 16) | (g << 8) | r);
    }

    public static void PopStyle()
    {
        if(_hasPushed)
        {
            _hasPushed = false;

            ImGui.PopStyleVar(19);

            if(_hasPushedColor)
            {
                _hasPushedColor = false;
                ImGui.PopStyleColor(51);
            }

            //if(_hasPushedScale)
            //    ImGui.GetIO().FontGlobalScale = _lastGlobalScale;
        }
    }
}
