using Brio.MCDF.API.Data.Enum;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Brio.MCDF.API.Data;

public class CharacterDataEX
{
    public Dictionary<ObjectKind, string> CustomizePlusScale { get; set; } = [];
    public Dictionary<ObjectKind, HashSet<FileReplacement>> FileReplacements { get; set; } = [];
    public Dictionary<ObjectKind, string> GlamourerString { get; set; } = [];
    public string HeelsData { get; set; } = string.Empty;
    public string HonorificData { get; set; } = string.Empty;
    public string ManipulationString { get; set; } = string.Empty;
    public string MoodlesData { get; set; } = string.Empty;
    public string PetNamesData { get; set; } = string.Empty;

    public void SetFragment(ObjectKind kind, CharacterDataFragment? fragment)
    {
        if(kind == ObjectKind.Player)
        {
            var playerFragment = fragment as CharacterDataFragmentPlayer;
            HeelsData = playerFragment?.HeelsData ?? string.Empty;
            HonorificData = playerFragment?.HonorificData ?? string.Empty;
            ManipulationString = playerFragment?.ManipulationString ?? string.Empty;
            MoodlesData = playerFragment?.MoodlesData ?? string.Empty;
            PetNamesData = playerFragment?.PetNamesData ?? string.Empty;
        }

        if(fragment is null)
        {
            CustomizePlusScale.Remove(kind);
            FileReplacements.Remove(kind);
            GlamourerString.Remove(kind);
        }
        else
        {
            CustomizePlusScale[kind] = fragment.CustomizePlusScale;
            FileReplacements[kind] = fragment.FileReplacements;
            GlamourerString[kind] = fragment.GlamourerString;
        }
    }

    public CharacterData ToAPI()
    {
        Dictionary<ObjectKind, List<FileReplacementData>> fileReplacements =
            FileReplacements.ToDictionary(k => k.Key, k => k.Value.Where(f => f.HasFileReplacement && !f.IsFileSwap)
            .GroupBy(f => f.Hash, StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                return new FileReplacementData()
                {
                    GamePaths = g.SelectMany(f => f.GamePaths).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
                    Hash = g.First().Hash,
                };
            }).ToList());

        foreach(var item in FileReplacements)
        {
            var fileSwapsToAdd = item.Value.Where(f => f.IsFileSwap).Select(f => f.ToFileReplacementDto());
            fileReplacements[item.Key].AddRange(fileSwapsToAdd);
        }

        return new CharacterData()
        {
            FileReplacements = fileReplacements,
            GlamourerData = GlamourerString.ToDictionary(d => d.Key, d => d.Value),
            ManipulationData = ManipulationString,
            HeelsData = HeelsData,
            CustomizePlusData = CustomizePlusScale.ToDictionary(d => d.Key, d => d.Value),
            HonorificData = HonorificData,
            MoodlesData = MoodlesData,
            PetNamesData = PetNamesData
        };
    }
}
