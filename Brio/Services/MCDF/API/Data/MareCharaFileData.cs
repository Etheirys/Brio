using Brio.MCDF.API.Data.Enum;
using Brio.MCDF.Game.FileCache;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Brio.MCDF.API.Data;

public record FileSwap(IEnumerable<string> GamePaths, string FileSwapPath);
public record FileData(IEnumerable<string> GamePaths, int Length, string Hash);

public class MareCharaFileData
{
    public string Description { get; set; } = string.Empty;
    public string GlamourerData { get; set; } = string.Empty;
    public string CustomizePlusData { get; set; } = string.Empty;
    public string ManipulationData { get; set; } = string.Empty;

    public List<FileData> Files { get; set; } = [];
    public List<FileSwap> FileSwaps { get; set; } = [];

    public MareCharaFileData() { }
    public MareCharaFileData(FileCacheService manager, string description, CharacterData dto)
    {
        Description = description;

        if(dto.GlamourerData.TryGetValue(ObjectKind.Player, out var glamourerData))
        {
            GlamourerData = glamourerData;
        }

        dto.CustomizePlusData.TryGetValue(ObjectKind.Player, out var customizePlusData);
        CustomizePlusData = customizePlusData ?? string.Empty;
        ManipulationData = dto.ManipulationData;

        if(dto.FileReplacements.TryGetValue(ObjectKind.Player, out var fileReplacements))
        {
            var grouped = fileReplacements.GroupBy(f => f.Hash, StringComparer.OrdinalIgnoreCase);

            foreach(var file in grouped)
            {
                if(string.IsNullOrEmpty(file.Key))
                {
                    foreach(var item in file)
                    {
                        FileSwaps.Add(new FileSwap(item.GamePaths, item.FileSwapPath));
                    }
                }
                else
                {
                    var filePath = manager.GetFileCacheByHash(file.First().Hash)?.ResolvedFilepath;
                    if(filePath != null)
                    {
                        Files.Add(new FileData(file.SelectMany(f => f.GamePaths), (int)new FileInfo(filePath).Length, file.First().Hash));
                    }
                }
            }
        }
    }

    public byte[] ToByteArray()
    {
        return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(this));
    }

    public static MareCharaFileData FromByteArray(byte[] data)
    {
        return JsonSerializer.Deserialize<MareCharaFileData>(Encoding.UTF8.GetString(data))!;
    }
}
