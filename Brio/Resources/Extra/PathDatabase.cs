using Brio.Core;
using MessagePack;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.IO.Hashing;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Brio.Resources.Extra;

public enum PathKind
{
    Model,
    Vfx,
}

[MessagePackObject]
public sealed class PathData
{
    [Key(0)] public string Path { get; set; } = string.Empty;

    [Key(1)] public string Name { get; set; } = string.Empty;
    [Key(2)] public string Description { get; set; } = string.Empty;
    [Key(3)] public long LastModifiedUTC { get; set; }

    [Key(4)] public string Expansion { get; set; } = string.Empty;

    [Key(5)] public int Length { get; set; } = 0;
    [Key(6)] public bool Repeats { get; set; } = false;
    [Key(7)] public bool RequiresRefresh { get; set; } = false;

    [Key(8)] public List<int> KnownTerritoryLocations { get; set; } = [];
    [Key(9)] public List<string> Subtypes { get; set; } = [];
    [Key(10)] public List<string> AssetType { get; set; } = [];
    [Key(11)] public List<string> Tags { get; set; } = [];

    public PathData Clone() => new()
    {
        Path = Path,
        Name = Name,
        Description = Description,
        LastModifiedUTC = LastModifiedUTC,
        Expansion = Expansion,
        Length = Length,
        Repeats = Repeats,
        RequiresRefresh = RequiresRefresh,
        KnownTerritoryLocations = [.. KnownTerritoryLocations],
        Subtypes = [.. Subtypes],
        AssetType = [.. AssetType],
        Tags = [.. Tags],
    };

    public bool ContentEquals(PathData other)
    {
        if(Name != other.Name) return false;
        if(Description != other.Description) return false;
        if(Expansion != other.Expansion) return false;
        if(Length != other.Length) return false;
        if(Repeats != other.Repeats) return false;
        if(RequiresRefresh != other.RequiresRefresh) return false;

        if(KnownTerritoryLocations.Count != other.KnownTerritoryLocations.Count) return false;
        for(var i = 0; i < KnownTerritoryLocations.Count; i++)
            if(KnownTerritoryLocations[i] != other.KnownTerritoryLocations[i]) return false;

        if(Subtypes.Count != other.Subtypes.Count) return false;
        for(var i = 0; i < Subtypes.Count; i++)
            if(Subtypes[i] != other.Subtypes[i]) return false;

        if(AssetType.Count != other.AssetType.Count) return false;
        for(var i = 0; i < AssetType.Count; i++)
            if(AssetType[i] != other.AssetType[i]) return false;

        if(Tags.Count != other.Tags.Count) return false;
        for(var i = 0; i < Tags.Count; i++)
            if(Tags[i] != other.Tags[i]) return false;

        return true;
    }

    public static string Normalize(string path)
    {
        if(string.IsNullOrEmpty(path))
            return string.Empty;

        var s = path.Replace('\\', '/').Trim();

        while(s.Contains("//"))
            s = s.Replace("//", "/");

        return s.Trim('/').ToLowerInvariant();
    }

    public static ulong Hash(string path)
    {
        var normalizedPath = Normalize(path);
        var maxBytes = Encoding.UTF8.GetMaxByteCount(normalizedPath.Length);

        Span<byte> buffer = new byte[maxBytes];
        int written = Encoding.UTF8.GetBytes(normalizedPath, buffer);
        return XxHash3.HashToUInt64(buffer[..written]);
    }

    public static string FileName(string pathOrName)
    {
        var norm = Normalize(pathOrName);
        if(norm.Length == 0)
            return string.Empty;

        var slash = norm.LastIndexOf('/');
        var name = slash >= 0 ? norm[(slash + 1)..] : norm;

        var dot = name.LastIndexOf('.');
        return dot > 0 ? name[..dot] : name;
    }
}

[MessagePackObject]
public sealed class PathStore
{
    public const int FormatVersion = 1;
    public static readonly byte[] Magic = "BRIOPATH"u8.ToArray();

    [Key(0)] public int KeyVersion { get; set; } = 1;
    [Key(1)] public Dictionary<ulong, PathData> Entries { get; set; } = [];
}

[MessagePackObject]
public sealed class PathMetaExport
{
    public const int FormatVersion = 1;
    public static readonly byte[] Magic = "BRIOPDBX"u8.ToArray();

    [Key(0)] public long ExportedUTC { get; set; }
    [Key(1)] public PathStore DB { get; set; } = new();
}

public readonly record struct GamePathInfo(string Path, string DisplayName, string Expansion, string Subtype, string AssetType);

public sealed partial class PathIndex
{
    public static readonly Dictionary<string, string> ExpansionNames = new()
    {
        { "ffxiv", "Realm Reborn"   },
        { "ex1",   "Heavensward"    },
        { "ex2",   "Stormblood"     },
        { "ex3",   "Shadowbringers" },
        { "ex4",   "Endwalker"      },
        { "ex5",   "Dawntrail"      },
        //{ "ex6",   "Evercold"      },
    };

    public static readonly Dictionary<string, string> AssetTypeNames = new()
    {
        { "rck",  "Rock"      }, { "rock", "Rock"      }, { "roc",  "Rock"          },
        { "wal",  "Wall"      }, { "wall", "Wall"      },
        { "tre",  "Tree"      }, { "tree", "Tree"      },
        { "dor",  "Door"      },
        { "cel",  "Ceiling "  },
        { "plr",  "Pillar"    }, { "pil",  "Pillar"    }, { "pill", "Pillar"        },
        { "flo",  "Floor"     },
        { "lmp",  "Lamp"      }, { "lamp", "Lamp"      }, { "ligt", "Light"         }, { "lig",  "Light"     },
        { "gat",  "Gate"      }, { "gate", "Gate"      },
        { "fen",  "Fence"     },
        { "tow",  "Tower"     },
        { "obj",  "Object"    },
        { "nat",  "Nature"    },
        { "cry",  "Crystal"   },
        { "wat",  "Water"     }, { "sea",  "Sea"       },
        { "stc",  "Structure" },
        { "gls",  "Glass"     }, { "grs",  "Glass"     },
        { "box",  "Box"       },
        { "flw",  "Flower"    },
        { "bos",  "Boss"      },
        { "wep",  "Weapon"    },
        { "fnt",  "Furniture"       },
        { "rub",  "Rubble"          },
        { "cin",  "Coins"           },
        { "lsf",  "Landscape"       },
        { "arf",  "Miscellaneous"   },
        { "ter",  "Terrain"   }, { "plt",  "Foliage"  }, { "bsh",  "Foliage"        },
        { "gren", "Foliage"   },
        { "itm",  "Item"      },
        { "chr",  "Chair"     }, { "chair", "Chair"   },
        { "dsk",  "Desk"      }, { "desk", "Desk"     },
        { "rug",  "Rug"       },
        { "slf",  "Shelf"     }, { "shelf", "Shelf"   }, { "she",  "Shelf"          },
        { "win",  "Window"    },
        { "tbl",  "Table"     }, { "table", "Table"   },
        { "bed",  "Bed"       },
        { "sof",  "Sofa"      }, { "sofa", "Sofa"     },
        { "cab",  "Cabinet"   },
        { "sign", "Sign"      }, { "sgn",  "Sign"     },
        { "stl",  "Stall"     },
        { "ban",  "Banner"    },
        { "pot",  "Pot"       },
        { "str",  "Stairs"    },
        { "brg",  "Bridge"    },
        { "rof",  "Roof"      },
        { "fsh",  "Fish"      },
        { "door", "Door"      },
        { "fnc",  "Fence"     }, { "fenc", "Fence"   },
        { "flr",  "Floor"     }, { "flor", "Floor"   },
        { "rom",  "Room"      }, { "room", "Room"    },
        { "bas",  "Base"      }, { "base", "Base"    },
        { "air",  "Airship"   },
        { "boss", "Boss"      },
        { "step", "Steps"     }, { "stp",  "Steps"   },
        { "pip",  "Pipe"      },
        { "pol",  "Pole"      },
        { "ivy",  "Ivy"       }, { "tuta",  "Ivy"    },
        { "dec",  "Decoration"},
        { "bui",  "Building"  },
        { "sak",  "Railing"   }, { "saku", "Railing" },
        { "stn",  "Stone"     },
        { "rok",  "Rock"      }, { "rk",   "Rock"    },
    };

    public static readonly Dictionary<string, string> SubtypeNames = new()
    {
        { "fld",    "Field"         },
        { "dun",    "Dungeon"       },
        { "twn",    "Town"          },
        { "rad",    "Raid"          },
        { "evt",    "Event"         },
        { "cnt",    "Content"       },
        { "btl",    "Nier"          },
        { "alx",    "Alexander"     },
        // { "ome",    ""         },
        { "bah",    "Bahamut"       },
        { "chr",    "Cinematic"     },
        { "pvp",    "PVP"           },
        { "ind",    "Indoor"        },
        { "ang",    "Arena"         },
        { "nature",    "Earth"      },
        //{ "xbm",    ""        },
        { "jai",    "Jail"          },
        { "common", "Common"        },
    };

    //

    private readonly GamePathInfo[] _paths;
    private readonly FrozenDictionary<ulong, int> _byHash;
    private readonly MultiValueDictionary<string, string> _byFileName;

    public int Count => _paths.Length;
    public IReadOnlyList<GamePathInfo> Paths => _paths;

    //

    [GeneratedRegex(@"^[a-z]+", RegexOptions.Compiled)]
    private static partial Regex LeadingAlphaRegex();

    //

    private PathIndex(GamePathInfo[] paths, Dictionary<ulong, int> byHash, MultiValueDictionary<string, string> byFileName)
    {
        _paths = paths;
        _byHash = byHash.ToFrozenDictionary();
        _byFileName = byFileName;
    }

    public static PathIndex FromLines(IEnumerable<string> lines)
    {
        var set = new SortedSet<string>(StringComparer.Ordinal);
        foreach(var line in lines)
        {
            var norm = PathData.Normalize(line);
            if(norm.Length > 0)
                set.Add(norm);
        }

        var infos = new GamePathInfo[set.Count];
        var byHash = new Dictionary<ulong, int>(infos.Length);
        var byFileName = new MultiValueDictionary<string, string>();
        var i = 0;
        foreach(var path in set)
        {
            var info = ParsePath(path);
            infos[i] = info;
            byHash[PathData.Hash(info.Path)] = i;
            byFileName.Add(PathData.FileName(info.Path), info.Path);
            i++;
        }

        return new PathIndex(infos, byHash, byFileName);
    }

    private static GamePathInfo ParsePath(string path)
    {
        var splitPath = path.Split('/');
        string expansion = "Base Game";
        string subtype = "Unknown";

        if(splitPath.Length > 1 && splitPath[0] == "bg")
        {
            if(ExpansionNames.TryGetValue(splitPath[1], out var exp))
            {
                expansion = exp;
                if(splitPath.Length > 3 && SubtypeNames.TryGetValue(splitPath[3], out var subtypeName))
                    subtype = subtypeName;
                else if(splitPath.Length > 3)
                    subtype = splitPath[3];
            }
        }
        else if(splitPath[0] == "bgcommon" && splitPath.Length > 2)
        {
            subtype = SubtypeNames.TryGetValue(splitPath[1], out var subeTypeName) ? subeTypeName : splitPath[1];
        }

        var fileName = Path.GetFileNameWithoutExtension(path);
        var assetType = GetAssetType(fileName);

        return new GamePathInfo(path, $"{assetType} [{fileName}]", expansion, subtype, assetType);
    }

    private static string GetAssetType(string fileName)
    {
        var parts = fileName.Split('_', StringSplitOptions.RemoveEmptyEntries);

        string? group = null;
        foreach(var part in parts)
        {
            var match = LeadingAlphaRegex().Match(part);
            var alpha = match.Success ? match.Value : string.Empty;
            if(alpha.Length >= 2 && AssetTypeNames.TryGetValue(alpha, out var typename))
            {
                group = typename;
                break;
            }
        }

        return group ?? "Other";
    }

    public static PathIndex FromFile(string path)
        => FromLines(File.ReadLines(path));

    public bool TryGetPath(ulong hash, out string path)
    {
        if(_byHash.TryGetValue(hash, out var i))
        {
            path = _paths[i].Path;
            return true;
        }
        path = string.Empty;

        return false;
    }

    public bool TryGetByFileName(string fileName, out IReadOnlyList<string> paths)
    {
        if(_byFileName.TryGetValues(PathData.FileName(fileName), out var list))
        {
            paths = list;
            return true;
        }
        paths = [];

        return false;
    }

    public bool Contains(string path)
        => _byHash.ContainsKey(PathData.Hash(path));

    public IEnumerable<string> WithPrefix(string prefix, int limit = 100)
    {
        var prefixNormalized = PathData.Normalize(prefix);

        var lo = LowerBound(prefixNormalized);
        var count = 0;

        for(var i = lo; i < _paths.Length && count < limit; i++)
        {
            if(!_paths[i].Path.StartsWith(prefixNormalized, StringComparison.Ordinal))
                break;
            yield return _paths[i].Path;
            count++;
        }
    }

    private int LowerBound(string key)
    {
        int lo = 0, hi = _paths.Length;
        while(lo < hi)
        {
            var mid = (lo + hi) >> 1;
            if(string.CompareOrdinal(_paths[mid].Path, key) < 0)
                lo = mid + 1;
            else
                hi = mid;
        }
        return lo;
    }

}

public sealed class PathDatabase
{
    private readonly PathIndex _modelIndex;
    private readonly PathIndex _vfxIndex;
    private readonly PathStore _pluginStore;
    private readonly PathStore _userStore;

    private readonly List<string> _modelSubtypeOptions = [];
    private readonly List<string> _modelExpansionOptions = [];

    public PathDatabase(PathIndex models, PathIndex vfx, PathStore pluginStore, PathStore userStore)
    {
        _modelIndex = models;
        _vfxIndex = vfx;
        _pluginStore = pluginStore;
        _userStore = userStore;

        _modelExpansionOptions = [.. _modelIndex.Paths.Select(m => m.Expansion).Distinct().Order()];
        _modelSubtypeOptions = [.. _modelIndex.Paths.Select(m => m.Subtype).Distinct().Order()];

    }

    public PathIndex Models => _modelIndex;
    public PathIndex Vfx => _vfxIndex;
    public PathIndex GetIndex(PathKind kind) => kind == PathKind.Vfx ? _vfxIndex : _modelIndex;

    public PathStore PluginStore => _pluginStore;
    public PathStore UserStore => _userStore;

    public List<string> ModelSubtypeOptions => _modelSubtypeOptions;
    public List<string> ModelExpansionOptions => _modelExpansionOptions;

    public PathData? GetPathDataByHash(ulong hash) =>
        _userStore.Entries.TryGetValue(hash, out var user) ? user
        : _pluginStore.Entries.TryGetValue(hash, out var plugin) ? plugin
        : null;
    public PathData? GetPathDataByPath(string path)
        => GetPathDataByHash(PathData.Hash(path));

    public IReadOnlyList<PathMeta> GetByFileName(PathKind kind, string fileName)
    {
        if(!GetIndex(kind).TryGetByFileName(fileName, out var paths))
            return [];

        var results = new List<PathMeta>(paths.Count);
        foreach(var path in paths)
        {
            var meta = GetPathDataByPath(path);
            if(meta is not null)
                results.Add(new PathMeta(path, meta));
        }
        return results;
    }
    public IReadOnlyList<string> GetPathsByFileName(PathKind kind, string fileName)
        => GetIndex(kind).TryGetByFileName(fileName, out var paths) ? paths : [];

    public void SetMetaData(string path, PathData meta)
        => SetMetaData(_userStore, path, meta);
    public bool RemoveMetaData(string path)
        => RemoveMetaData(_userStore, path);

    public void SetMetaData(PathStore store, string path, PathData meta)
    {
        meta.LastModifiedUTC = DateTime.UtcNow.Ticks;
        store.Entries[PathData.Hash(path)] = meta;
    }
    public bool RemoveMetaData(PathStore store, string path)
        => store.Entries.Remove(PathData.Hash(path));

    //

    public PathMetaExport Export()
    {
        var delta = new PathStore
        {
            KeyVersion = _userStore.KeyVersion,
        };

        foreach(var items in _userStore.Entries)
        {
            if(_pluginStore.Entries.TryGetValue(items.Key, out var pluginMeta)
                && items.Value.ContentEquals(pluginMeta))
                continue;

            delta.Entries[items.Key] = items.Value.Clone();
        }

        return new PathMetaExport
        {
            ExportedUTC = DateTime.UtcNow.Ticks,
            DB = delta,
        };
    }

    //

    public enum ConflictPolicy
    {
        LastWriteWins,
        PreferIncoming,
        PreferExisting,
    }

    public static MergeResult MergeStore(PathStore target, PathMetaExport import, ConflictPolicy policy = ConflictPolicy.LastWriteWins)
    {
        if(import.DB.KeyVersion != target.KeyVersion)
            throw new InvalidDataException(
                $"Key version mismatch: import={import.DB.KeyVersion}, target={target.KeyVersion}. ");

        var result = new MergeResult();
        foreach(var item in import.DB.Entries)
        {
            if(!target.Entries.TryGetValue(item.Key, out var existing))
            {
                target.Entries[item.Key] = item.Value.Clone();
                result.Added++;
                continue;
            }

            var take = policy switch
            {
                ConflictPolicy.PreferIncoming => true,
                ConflictPolicy.PreferExisting => false,
                _ => item.Value.LastModifiedUTC > existing.LastModifiedUTC,
            };

            if(take)
            {
                target.Entries[item.Key] = item.Value.Clone();
                result.Updated++;
            }
            else
            {
                result.Skipped++;
            }
        }
        return result;
    }

    //

    public static (PathIndex Models, PathIndex Vfx) LoadIndexes(Stream stream)
    {
        PathsFile? data = System.Text.Json.JsonSerializer.Deserialize<PathsFile>(stream, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

        if(data == null)
            return (PathIndex.FromLines([]), PathIndex.FromLines([]));

        return (PathIndex.FromLines(data.MdlPaths), PathIndex.FromLines(data.AvfxPaths));
    }

    public static PathDatabase LoadFromGz(Stream file, PathStore pluginStore, PathStore userStore)
    {
        using var decompress = new GZipStream(file, CompressionMode.Decompress);
        var (models, vfx) = LoadIndexes(decompress);
        return new PathDatabase(models, vfx, pluginStore, userStore);
    }

    //

    private static readonly MessagePackSerializerOptions SerializerOptions =
        MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray);

    public static byte[] Serialize<T>(T value, byte[] magic, int version)
    {
        var payload = MessagePackSerializer.Serialize(value, SerializerOptions);

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write(magic);
        writer.Write(version);
        writer.Write(payload.Length);
        writer.Write(payload);

        writer.Flush();
        return stream.ToArray();
    }

    public static T Deserialize<T>(byte[] bytes, byte[] magic)
    {
        if(!HasMagic(bytes, magic))
            throw new InvalidDataException("Not a valid file (bad MAGIC).");

        using var stream = new MemoryStream(bytes);
        using var reader = new BinaryReader(stream);

        reader.ReadBytes(magic.Length);
        _ = reader.ReadInt32();
        var length = reader.ReadInt32();
        var payload = reader.ReadBytes(length);

        return MessagePackSerializer.Deserialize<T>(payload, SerializerOptions);
    }

    private static bool HasMagic(byte[] data, byte[] magic)
    {
        if(data.Length < magic.Length)
            return false;

        for(int i = 0; i < magic.Length; i++)
        {
            if(data[i] != magic[i])
                return false;
        }

        return true;
    }

    public static void Save<T>(string path, T value, byte[] magic, int version)
    {
        var bytes = Serialize(value, magic, version);
        var dir = Path.GetDirectoryName(Path.GetFullPath(path))!;
        Directory.CreateDirectory(dir);

        var tmp = Path.Combine(dir, Path.GetFileName(path) + ".tmp");
        File.WriteAllBytes(tmp, bytes);
        File.Move(tmp, path, overwrite: true);
    }
}

public sealed class PathsFile
{
    public List<string> MdlPaths { get; set; } = [];
    public List<string> AvfxPaths { get; set; } = [];
}

public readonly record struct PathMeta(string Path, PathData Data);

public struct MergeResult
{
    public int Added;
    public int Updated;
    public int Skipped;

    public override readonly string ToString()
        => $"added={Added}, updated={Updated}, skipped={Skipped}";
}
