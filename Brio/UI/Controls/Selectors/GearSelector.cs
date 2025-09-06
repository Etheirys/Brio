using Brio.Game.Actor.Appearance;
using Brio.Resources;
using Brio.Resources.Extra;
using Brio.UI.Controls.Stateless;
using Dalamud.Bindings.ImGui;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using System;
using System.Numerics;

namespace Brio.UI.Controls.Selectors;

public class GearSelector(string id) : Selector<ModelDatabase.ModelInfo>(id)
{
    protected override Vector2 MinimumListSize { get; } = new(300, 300);

    protected override float EntrySize => ImGui.GetTextLineHeight() * 3.2f;
    protected virtual Vector2 IconSize => new(ImGui.GetTextLineHeight() * 3f);

    protected override SelectorFlags Flags => SelectorFlags.AllowSearch | SelectorFlags.ShowOptions | SelectorFlags.AdaptiveSizing;

    private ActorEquipSlot _allowedSlots = ActorEquipSlot.None;

    protected override void PopulateList()
    {
        var models = GameDataProvider.Instance.ModelDatabase.GetAllGear();
        AddItems(models);
    }

    public void SetGearSelect(ModelDatabase.ModelInfo? item, ActorEquipSlot slots)
    {
        _allowedSlots = slots;
        Select(item, true, true, true);
    }

    protected override bool Filter(ModelDatabase.ModelInfo item, string search)
    {
        if((_allowedSlots & item.Slots) == 0)
            return false;

        WeaponModelId weaponModelId = default;
        weaponModelId.Value = item.ModelId;
        string searchTerm = $"{item.Name} {item.ModelId}";

        if(searchTerm.Contains(search, StringComparison.InvariantCultureIgnoreCase))
            return true;

        return false;
    }

    protected override int Compare(ModelDatabase.ModelInfo itemA, ModelDatabase.ModelInfo itemB)
    {
        if(itemA.ModelId == 0 && itemB.ModelId != 0)
            return -1;

        if(itemA.ModelId != 0 && itemB.ModelId == 0)
            return 1;

        if(!string.IsNullOrEmpty(itemA.Name) && string.IsNullOrEmpty(itemB.Name))
            return -1;

        if(string.IsNullOrEmpty(itemA.Name) && !string.IsNullOrEmpty(itemB.Name))
            return 1;

        return string.Compare(itemA.Name, itemB.Name, StringComparison.InvariantCultureIgnoreCase);
    }

    protected override void DrawItem(ModelDatabase.ModelInfo item, bool isHovered)
    {
        ImBrio.BorderedGameIcon("icon", item.Icon, _allowedSlots.GetEquipSlotFallback(), flags: ImGuiButtonFlags.None, size: IconSize);

        ImGui.SameLine();

        string modelId;

        if(_allowedSlots.HasFlag(ActorEquipSlot.MainHand) || _allowedSlots.HasFlag(ActorEquipSlot.OffHand))
        {
            WeaponModelId weaponModelId = new()
            {
                Value = item.ModelId
            };

            weaponModelId.Value = item.ModelId;
            modelId = $"{weaponModelId.Id}, {weaponModelId.Type}, {weaponModelId.Variant}";
        }
        else
        {
            EquipmentModelId equipmentModelId = new()
            {
                Value = (uint)item.ModelId
            };
            modelId = $"{equipmentModelId.Id}, {equipmentModelId.Variant}";
        }
        if(_allowedSlots.HasFlag(ActorEquipSlot.Prop))
            ImGui.Text($"{item.Name}\n{modelId}");
        else
            ImGui.Text($"{item.Name}\n{modelId}\n{item.Slots}");
    }


}
