using Brio.Core;
using Brio.Game.Actor.Appearance;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using Lumina.Excel.Sheets;
using System.Collections.Generic;
using System.Linq;

namespace Brio.Resources.Extra;

public class ModelDatabase
{
    private readonly MultiValueDictionary<ulong, ModelInfo> _modelLookupTable;
    private readonly List<ModelInfo> _modelsList;

    public ModelDatabase(ResourceProvider _resourceProvider)
    {
        _modelLookupTable = new();
        _modelsList = [];


        // From Game
        var items = GameDataProvider.Instance.Items.Values;
        foreach(var item in items)
        {
            var slots = item.EquipSlotCategory.ValueNullable?.GetEquipSlots() ?? ActorEquipSlot.None;
            if(slots != ActorEquipSlot.None)
            {
                var modelInfo = new ModelInfo(item.ModelMain, item.RowId, item.Name.ToString(), item.Icon, slots, item);
                AddModel(modelInfo);

                if(item.ModelSub != 0)
                {
                    modelInfo = new ModelInfo(item.ModelSub, item.RowId, item.Name.ToString(), item.Icon, ActorEquipSlot.OffHand, item);
                    AddModel(modelInfo);
                }
            }
        }

        // From JSON
        var knownEntries = _resourceProvider.GetResourceDocument<List<PropsFileEntry>>("Data.Props.json");
        knownEntries = knownEntries.GroupBy(x => x.Name).Select(g => g.First()).ToList();
        foreach(var item in knownEntries)
        {
            ushort[] result = item.Id.Split(", ").Select(ushort.Parse).ToArray();
            if(result.Length > 2)
            {
                WeaponModelId weaponItem = new() { Id = result[0], Type = result[1], Variant = (byte)result[2] };
                var modelInfo = new ModelInfo(weaponItem.Value, 0, $"{item.Name}\n{item.Description?.ToString()}", 0, ActorEquipSlot.Prop, null);
                AddModel(modelInfo);
            }
            else
            {
                EquipmentModelId actualItem = new() { Id = result[0], Variant = (byte)result[1] };
                var modelInfo = new ModelInfo(actualItem.Value, 0, $"{item.Name}\n{item.Description?.ToString()}", 0, ActorEquipSlot.Prop, null);
                AddModel(modelInfo);
            }
        }

        // Special
        var none = new ModelInfo(0, 0, "None", 0, ActorEquipSlot.All, null);
        AddModel(none);

        var smallclothes = new ModelInfo(SpecialAppearances.Smallclothes.Value, 0, "Smallclothes", 0, ActorEquipSlot.AllButWeapons, null);
        AddModel(smallclothes);
    }

    public ModelInfo? GetModelById(ulong modelId, ActorEquipSlot slot)
    {
        if(_modelLookupTable.TryGetValues(modelId & 0x00FFFFFFFFFFFFFF, out var values))
            return values.FirstOrDefault(x => x != null && (x.Slots & slot) != 0, null);

        return null;
    }
    public ModelInfo? GetModelById(WeaponModelId modelId, ActorEquipSlot slot) => GetModelById(modelId.Value, slot);
    public ModelInfo? GetModelById(EquipmentModelId modelId, ActorEquipSlot slot) => GetModelById((ulong)modelId.Value & 0x00FFFFFF, slot);

    public IEnumerable<ModelInfo> GetEquippableInSlots(ActorEquipSlot slots)
    {
        List<ModelInfo> models = [];
        foreach(var model in _modelsList)
        {
            if(model.Slots.HasFlag(slots))
                models.Add(model);
        }
        return models;
    }

    public IEnumerable<ModelInfo> GetAllGear() => _modelsList;

    private void AddModel(ModelInfo info)
    {
        _modelsList.Add(info);
        _modelLookupTable.Add(info.ModelId, info);
    }

    public record class ModelInfo(ulong ModelId, uint ItemId, string Name, uint Icon, ActorEquipSlot Slots, Item? Item);

    private class PropsFileEntry
    {
        public string Id { get; set; }
        public string Name { get; set; } = null!;
        public string Description { get; set; }
        public string Slot { get; set; }
    }

}
