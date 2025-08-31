using System;

namespace Brio.MCDF.Game.FileCache;

public class FileCacheEntity(string hash, string path, string lastModifiedDateTicks, long? size = null, long? compressedSize = null)
{
    public long? CompressedSize { get; set; } = compressedSize;
    public string CsvEntry => $"{Hash}{FileCacheService.CsvSplit}{PrefixedFilePath}{FileCacheService.CsvSplit}{LastModifiedDateTicks}|{Size ?? -1}|{CompressedSize ?? -1}";
    public string Hash { get; set; } = hash;
    public bool IsCacheEntry => PrefixedFilePath.StartsWith(FileCacheService.CachePrefix, StringComparison.OrdinalIgnoreCase);
    public string LastModifiedDateTicks { get; set; } = lastModifiedDateTicks;
    public string PrefixedFilePath { get; init; } = path;
    public string ResolvedFilepath { get; private set; } = string.Empty;
    public long? Size { get; set; } = size;

    public void SetResolvedFilePath(string filePath)
    {
        ResolvedFilepath = filePath.ToLowerInvariant().Replace("\\\\", "\\", StringComparison.Ordinal);
    }
}
