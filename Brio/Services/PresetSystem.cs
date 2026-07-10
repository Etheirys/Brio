using Brio.Entities.Camera;
using Brio.Entities.World;
using Brio.Game.World;
using Brio.Services.Models;
using Dalamud.Plugin;
using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

namespace Brio.Services;

public enum PresetType : int
{
    Light = 1,
    Camera = 2,
}

[MessagePackObject]
public class BrioPresets
{
    [Key(0)] public List<Preset> Presets { get; set; } = [];
}

[MessagePackObject]
public record class Preset
{
    [Key(0)] public int Version { get; set; } = 1;

    [Key(1)] public required string Name { get; set; }
    [Key(2)] public string? Description { get; set; }

    [Key(3)] public required string Path { get; set; }
    [Key(4)] public required PresetType Type { get; set; }
    [Key(5)] public int EntryCount { get; set; }

    [Key(6)] public DateTime? Created { get; set; }
}

public class PresetSystem
{
    private const int FormatVersion = 1;
    private static readonly byte[] Magic = "BRIOPST"u8.ToArray();

    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly LightingService _lightingService;

    private readonly Dictionary<PresetType, BrioPresets> _presets = [];

    public PresetSystem(IDalamudPluginInterface pluginInterface, LightingService lightingService)
    {
        _pluginInterface = pluginInterface;
        _lightingService = lightingService;

        foreach(PresetType type in Enum.GetValues<PresetType>())
        {
            Directory.CreateDirectory(PresetSaveFolder(type));

            LoadPresetData(type);
        }
    }

    private string PresetSaveFolder(PresetType type)
        => Path.Combine(_pluginInterface.GetPluginConfigDirectory(), "Data", "Presets", type.ToString());
    private string BrioDataPath(PresetType type)
        => Path.Combine(PresetSaveFolder(type), "brio.data");

    public IReadOnlyList<Preset> GetPresets(PresetType type)
        => _presets[type].Presets;

    //

    public void SaveLightPreset(string name, string? description, IReadOnlyList<LightEntity> entities)
    {
        var dtos = entities
            .Select(entity => LightDTO.ToDTO(entity, _lightingService, Vector3.Zero))
            .Where(dto => dto is not null)
            .Cast<LightDTO>()
            .ToList();

        SavePreset(PresetType.Light, name, description, dtos);
    }
    public void SaveCameraPreset(string name, string? description, IReadOnlyList<CameraEntity> entities)
    {
        var dtos = entities
            .Select(entity => new CameraDTO { CameraType = entity.CameraType, Camera = entity.VirtualCamera })
            .ToList();

        SavePreset(PresetType.Camera, name, description, dtos);
    }

    public List<LightDTO> LoadLightPreset(Preset preset)
        => LoadPreset<LightDTO>(preset);
    public List<CameraDTO> LoadCameraPreset(Preset preset)
        => LoadPreset<CameraDTO>(preset);

    //

    private void SavePreset<T>(PresetType type, string name, string? description, List<T> dtos)
    {
        var path = Path.Combine(PresetSaveFolder(type), $"{name}-{DateTime.Now:yyyy-MM-dd-hh-mm-ss}.brioprst");

        try
        {
            Brio.Log.Verbose($"saving new preset: {path}");

            byte[] bytes = Serialize(dtos);
            File.WriteAllBytes(path, bytes);

            _presets[type].Presets.Add(new Preset
            {
                Name = name,
                Path = path,
                Description = description,
                Type = type,
                EntryCount = dtos.Count,
                Created = DateTime.UtcNow
            });

            SavePresetData(type);
        }
        catch(Exception ex)
        {
            Brio.Log.Error(ex, $"Exception while saving new preset: {name}");
        }
    }
    private List<T> LoadPreset<T>(Preset preset)
        => Deserialize<T>(File.ReadAllBytes(preset.Path));
    public void DeletePreset(Preset preset)
    {
        if(File.Exists(preset.Path))
            File.Delete(preset.Path);

        _presets[preset.Type].Presets.Remove(preset);

        SavePresetData(preset.Type);
    }

    //

    private void LoadPresetData(PresetType type)
    {
        var path = BrioDataPath(type);

        if(File.Exists(path))
            _presets[type] = MessagePackSerializer.Deserialize<BrioPresets>(File.ReadAllBytes(path));
        else
            _presets[type] = new BrioPresets();
    }
    private void SavePresetData(PresetType type)
    {
        byte[] bytes = MessagePackSerializer.Serialize(_presets[type]);

        File.WriteAllBytes(BrioDataPath(type), bytes);
    }

    private static byte[] Serialize<T>(List<T> entries)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        var payload = MessagePackSerializer.Serialize(entries);

        writer.Write(Magic);
        writer.Write(FormatVersion);
        writer.Write(payload.Length);
        writer.Write(payload);

        writer.Flush();
        return stream.ToArray();
    }
    private static List<T> Deserialize<T>(byte[] data)
    {
        using var stream = new MemoryStream(data);
        using var reader = new BinaryReader(stream);

        reader.ReadBytes(Magic.Length);
        _ = reader.ReadInt32();
        var length = reader.ReadInt32();
        var payload = reader.ReadBytes(length);

        return MessagePackSerializer.Deserialize<List<T>>(payload);
    }
}
