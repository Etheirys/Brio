using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using System.Numerics;

namespace Brio.UI.Controls.Stateless;

internal static partial class ImBrio
{
    public static bool DrawIconSelector(string id, IconSelectorEntry[] entries, ref int selectedId, int columns = 4, Vector2? iconSize = null, string fallbackImage = "Images.UnknownIcon.png", bool bitField = false)
    {
        iconSize ??= new Vector2(ImGui.GetTextLineHeight() * 3);

        bool wasClicked = false;

        for (int i = 0; i < entries.Length; ++i)
        {
            var entry = entries[i];

            var beginPos = ImGui.GetCursorScreenPos();

            if (entry.Icon == 0 && entry.FallbackOverride != null)
                fallbackImage = entry.FallbackOverride;

            if (BorderedGameIcon($"{id}_{i}", entry.Icon, fallbackImage, size: iconSize.Value))
            {
                wasClicked |= true;

                if (!bitField)
                    selectedId = entry.Id;
                else
                    selectedId ^= entry.Id;
            }

            var text = $"{entry.Id}";
            var textSize = ImGui.CalcTextSize(text);
            var textPos = new Vector2(beginPos.X + (iconSize.Value.X / 2) - textSize.X / 2, beginPos.Y);

            ImGui.GetWindowDrawList().AddText(textPos, 0xFF000000, text);

            if ((!bitField && selectedId == entry.Id) || (bitField && ((entry.Id & selectedId) != 0)))
            {
                using (ImRaii.PushFont(UiBuilder.IconFont))
                {
                    var starText = FontAwesomeIcon.Star.ToIconString();
                    var starSize = ImGui.CalcTextSize(starText);
                    var selectedPos = new Vector2(beginPos.X + iconSize.Value.X - starSize.X, beginPos.Y + iconSize.Value.Y - (starSize.Y * 1.1f));
                    ImGui.GetWindowDrawList().AddText(selectedPos, 0xFFFFFFFF, starText);
                }
            }

            if ((i + 1) % columns != 0)
                ImGui.SameLine();

        }

        return wasClicked;
    }

    public static bool DrawIconSelectorPopup(string id, IconSelectorEntry[] entries, ref int selectedId, int columns = 4, Vector2? iconSize = null, string fallbackImage = "Images.UnknownIcon.png", bool bitField = false)
    {
        bool wasChanged = false;

        using (var popup = ImRaii.Popup(id))
        {
            if (popup.Success)
            {
                ImGui.SetNextItemWidth(-1);
                wasChanged |= ImGui.InputInt($"##{id}_index", ref selectedId, 1, 1, ImGuiInputTextFlags.EnterReturnsTrue);
                wasChanged |= DrawIconSelector(id, entries, ref selectedId, columns, iconSize, fallbackImage, bitField);

                if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                    ImGui.CloseCurrentPopup();
            }
        }

        return wasChanged;
    }

    public record struct IconSelectorEntry(int Id, uint Icon, string? FallbackOverride = null);
}
