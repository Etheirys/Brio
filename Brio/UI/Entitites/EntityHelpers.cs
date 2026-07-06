using Brio.Entities.Core;
using Brio.UI.Controls.Stateless;
using Brio.UI.Widgets.Core;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace Brio.UI.Entitites;

public static class EntityHelpers
{
    public static void DrawEntitySection(Entity? entity, bool isUndocked = false, bool drawChild = false)
    {
        if(entity is not null && entity.IsAttached)
        {
            var capabilities = entity.Capabilities;

            if(entity.IsLoading)
            {
                DrawSpinner();
            }

            using(ImRaii.Disabled(entity.IsLoading))
            {
                using(ImRaii.PushId($"quickicons_{entity.Id}"))
                {
                    WidgetHelpers.DrawQuickIcons(capabilities);
                }

                if(entity.IsWidgetBodyHidden is false && isUndocked == false)
                {
                    ImBrio.VerticalPadding(2f);

                    if(drawChild)
                    {
                        ImGui.Separator();
                        ImBrio.VerticalPadding(2f);

                        using(var s = ImRaii.PushStyle(ImGuiStyleVar.ChildBorderSize, 0f))
                        using(var child = ImRaii.Child("###entitySection", new Vector2(-1, -1), false))
                        {
                            if(child.Success)
                            {
                                s.Pop();

                                using(ImRaii.PushId($"bodies_{entity.Id}"))
                                {
                                    WidgetHelpers.DrawBodies(capabilities);
                                }
                            }
                        }
                        return;
                    }

                    using(ImRaii.PushId($"bodies_{entity.Id}"))
                    {
                        WidgetHelpers.DrawBodies(capabilities);
                    }
                }
            }
        }
    }

    private static float spinnerAngle = 0;
    public static void DrawSpinner()
    {
        var cursor = ImGui.GetCursorPos();
        ImGui.SetCursorPosX((ImGui.GetWindowWidth() / 2) - 24);
        ImGui.SetCursorPosY((ImGui.GetWindowHeight() / 2) - 24);
        ImBrio.Spinner(ref spinnerAngle);
        ImGui.SetCursorPos(cursor);
    }
}
