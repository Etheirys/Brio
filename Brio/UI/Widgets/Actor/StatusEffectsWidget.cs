using Brio.Capabilities.Actor;
using Brio.Resources;
using Brio.UI.Controls.Selectors;
using Brio.UI.Controls.Stateless;
using Brio.UI.Widgets.Core;
using Dalamud.Interface;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using System.Linq;
using System.Numerics;

namespace Brio.UI.Widgets.Actor;

internal class StatusEffectsWidget(StatusEffectCapability capability) : Widget<StatusEffectCapability>(capability)
{
    public override string HeaderName => "Status Effects";

    public override WidgetFlags Flags => WidgetFlags.DrawBody;

    private int _selectedStatus;

    private static readonly StatusEffectSelector _globalStatusEffectSelector = new("global_status_selector");

    public override void DrawBody()
    {
        var statuses = Capability.ActiveStatuses;

        ImGui.SetNextItemWidth(-1);
        using(var listbox = ImRaii.ListBox("###status_effects", new Vector2(0, ImGui.GetTextLineHeight() * 6)))
        {
            if(listbox.Success)
            {
                foreach(var status in statuses)
                {
                    bool selected = status.RowId == _selectedStatus;

                    IDalamudTextureWrap? tex = null;
                    if(status.Icon != 0)
                        tex = UIManager.Instance.TextureProvider.GetIcon(status.Icon);
                    if(tex == null)
                        tex = ResourceProvider.Instance.GetResourceImage("Images.StatusEffect.png");

                    float ratio = tex.Size.X / tex.Size.Y;
                    float desiredHeight = ImGui.GetTextLineHeight() * 2f;
                    Vector2 iconSize = new(desiredHeight * ratio, desiredHeight);

                    var position = ImGui.GetCursorPos();
                    bool wasSelected = ImGui.Selectable($"###status_effects_{status.RowId}", selected, ImGuiSelectableFlags.None, new Vector2(0, desiredHeight));
                    ImGui.SetCursorPos(position);
                    ImGui.Image(tex.ImGuiHandle, iconSize);
                    ImGui.SameLine();
                    ImGui.Text($"{status.Name}\n{status.RowId}");

                    if(wasSelected)
                        _selectedStatus = (int)status.RowId;
                }
            }
        }

        bool isSelectedPlaying = _selectedStatus != 0 && statuses.Count((i) => _selectedStatus == i.RowId) > 0;
        bool canAdd = _selectedStatus != 0;

        ImGui.SetNextItemWidth(ImGui.CalcTextSize("XXXXXXX").X);
        ImGui.InputInt("###status_selected_input", ref _selectedStatus, 0, 0);
        if(ImBrio.IsItemConfirmed())
        {
            ApplyStatusEffect();
        }

        ImGui.SameLine();

        if(ImBrio.FontIconButton("status_effects_add", FontAwesomeIcon.Plus, "Add Effect", canAdd))
            ApplyStatusEffect();

        ImGui.SameLine();

        if(ImBrio.FontIconButton("status_effects_remove", FontAwesomeIcon.Minus, "Remove Effect", isSelectedPlaying))
            Capability.RemoveStatus((ushort)_selectedStatus);

        ImGui.SameLine();


        if(ImBrio.FontIconButton("status_effects_search", FontAwesomeIcon.Search, "Search"))
        {
            _globalStatusEffectSelector.Select(null, false);
            ImGui.OpenPopup("status_effect_search");
        }



        using(var popup = ImRaii.Popup("status_effect_search"))
        {
            if(popup.Success)
            {
                _globalStatusEffectSelector.Draw();

                if(_globalStatusEffectSelector.SoftSelectionChanged && _globalStatusEffectSelector.SoftSelected != null)
                {
                    _selectedStatus = (int)_globalStatusEffectSelector.SoftSelected.RowId;
                }

                if(_globalStatusEffectSelector.SelectionChanged && _globalStatusEffectSelector.Selected != null)
                {
                    _selectedStatus = (int)_globalStatusEffectSelector.Selected.RowId;
                    ApplyStatusEffect();
                    ImGui.CloseCurrentPopup();
                }
            }
        }
    }

    private void ApplyStatusEffect()
    {
        if(_selectedStatus == 0)
            return;

        Capability.AddStatus((ushort)_selectedStatus);
    }
}
