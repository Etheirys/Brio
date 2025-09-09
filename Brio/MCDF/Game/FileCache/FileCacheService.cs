using Brio.Config;
using Brio.IPC;
using Brio.MCDF.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;

namespace Brio.MCDF.Game.FileCache;

public class FileCacheService : IDisposable
{
    public const string CachePrefix = "{cache}";
    public const string PenumbraPrefix = "{penumbra}";
    public const string CsvSplit = "|";

    private readonly ConfigurationService _configurationService;
    private readonly PenumbraService _penumbraService;

    private readonly ConcurrentDictionary<string, List<FileCacheEntity>> _fileCaches = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim _getCachesByPathsSemaphore = new(1, 1);
    private readonly object _fileWriteLock = new();
    private readonly string _csvPath;

    public string TempPath => Path.Join(Path.GetTempPath(), "Brio");

    public string CacheFolder => GetTempPath(); //_configurationService.Configuration.MCDF.CacheFolder;

    public FileCacheService(ConfigurationService configurationService, PenumbraService penumbraService)
    {
        _configurationService = configurationService;
        _penumbraService = penumbraService;

        _csvPath = Path.Combine(CacheFolder, "FileCache.csv");

        //if(_configurationService.Configuration.MCDF.CacheFolder.IsNullOrEmpty())
        //{
        //    _configurationService.Configuration.MCDF.CacheFolder = TempPath;
        //}
    }

    private string GetTempPath()
    {
        if(Directory.Exists(TempPath) is false)
        {
            Directory.CreateDirectory(TempPath);
        }

        return TempPath;
    }
    public void ClearTemp()
    {
        if(Directory.Exists(TempPath) is false)
        {
            Directory.Delete(TempPath, true);
        }
    }

    private void AddHashedFile(FileCacheEntity fileCache)
    {
        if(!_fileCaches.TryGetValue(fileCache.Hash, out var entries) || entries is null)
        {
            _fileCaches[fileCache.Hash] = entries = [];
        }

        if(!entries.Exists(u => string.Equals(u.PrefixedFilePath, fileCache.PrefixedFilePath, StringComparison.OrdinalIgnoreCase)))
        {
            //Brio.Log.Debug("Adding to DB: {hash} => {path}", fileCache.Hash, fileCache.PrefixedFilePath);
            entries.Add(fileCache);
        }
    }
    public void RemoveHashedFile(string hash, string prefixedFilePath)
    {
        if(_fileCaches.TryGetValue(hash, out var caches))
        {
            var removedCount = caches?.RemoveAll(c => string.Equals(c.PrefixedFilePath, prefixedFilePath, StringComparison.Ordinal));
            Brio.Log.Debug("Removed from DB: {count} file(s) with hash {hash} and file cache {path}", removedCount!, hash, prefixedFilePath);

            if(caches?.Count == 0)
            {
                _fileCaches.Remove(hash, out var entity);
            }
        }
    }

    private FileCacheEntity? Validate(FileCacheEntity fileCache)
    {
        var file = new FileInfo(fileCache.ResolvedFilepath);
        if(!file.Exists)
        {
            RemoveHashedFile(fileCache.Hash, fileCache.PrefixedFilePath);
            return null;
        }

        if(!string.Equals(file.LastWriteTimeUtc.Ticks.ToString(CultureInfo.InvariantCulture), fileCache.LastModifiedDateTicks, StringComparison.Ordinal))
        {
            UpdateHashedFile(fileCache);
        }

        return fileCache;
    }

    public void UpdateHashedFile(FileCacheEntity fileCache, bool computeProperties = true)
    {
        //Brio.Log.Debug("Updating hash for {path}", fileCache.ResolvedFilepath);
        var oldHash = fileCache.Hash;
        var prefixedPath = fileCache.PrefixedFilePath;
        if(computeProperties)
        {
            var fi = new FileInfo(fileCache.ResolvedFilepath);
            fileCache.Size = fi.Length;
            fileCache.CompressedSize = null;
            fileCache.Hash = Crypto.GetFileHash(fileCache.ResolvedFilepath);
            fileCache.LastModifiedDateTicks = fi.LastWriteTimeUtc.Ticks.ToString(CultureInfo.InvariantCulture);
        }
        RemoveHashedFile(oldHash, prefixedPath);
        AddHashedFile(fileCache);
    }

    public FileCacheEntity? CreateFileEntry(string path)
    {
        FileInfo fi = new(path);
        if(!fi.Exists) return null;
        //Brio.Log.Verbose("Creating file entry for {path}", path);
        var fullName = fi.FullName.ToLowerInvariant();
        if(!fullName.Contains(_penumbraService.ModDirectory!.ToLowerInvariant(), StringComparison.Ordinal)) return null;
        string prefixedPath = fullName.Replace(_penumbraService.ModDirectory!.ToLowerInvariant(), PenumbraPrefix + "\\", StringComparison.Ordinal).Replace("\\\\", "\\", StringComparison.Ordinal);
        return CreateFileCacheEntity(fi, prefixedPath);
    }
    public FileCacheEntity? CreateCacheEntry(string path)
    {
        FileInfo fi = new(path);
        if(!fi.Exists) return null;
        //Brio.Log.Verbose("Creating cache entry for {path}", path);
        var fullName = fi.FullName.ToLowerInvariant();
        if(!fullName.Contains(CacheFolder.ToLowerInvariant(), StringComparison.Ordinal)) return null;
        string prefixedPath = fullName.Replace(CacheFolder.ToLowerInvariant(), CachePrefix + "\\", StringComparison.Ordinal).Replace("\\\\", "\\", StringComparison.Ordinal);
        return CreateFileCacheEntity(fi, prefixedPath);
    }

    private FileCacheEntity? CreateFileCacheEntity(FileInfo fileInfo, string prefixedPath, string? hash = null)
    {
        hash ??= Crypto.GetFileHash(fileInfo.FullName);
        var entity = new FileCacheEntity(hash, prefixedPath, fileInfo.LastWriteTimeUtc.Ticks.ToString(CultureInfo.InvariantCulture), fileInfo.Length);
        entity = ReplacePathPrefixes(entity);
        AddHashedFile(entity);
        lock(_fileWriteLock)
        {
            File.AppendAllLines(_csvPath, [entity.CsvEntry]);
        }
        var result = GetFileCacheByPath(fileInfo.FullName);
        //Brio.Log.Verbose("Creating cache entity for {name} success: {success}", fileInfo.FullName, (result != null));
        return result;
    }

    public FileCacheEntity? GetFileCacheByHash(string hash)
    {
        if(_fileCaches.TryGetValue(hash, out var hashes))
        {
            var item = hashes.OrderBy(p => p.PrefixedFilePath.Contains(PenumbraPrefix) ? 0 : 1).FirstOrDefault();
            if(item != null) return GetValidatedFileCache(item);
        }
        return null;
    }
    private FileCacheEntity? GetFileCacheByPath(string path)
    {
        var cleanedPath = path.Replace("/", "\\", StringComparison.OrdinalIgnoreCase).ToLowerInvariant()
            .Replace(_penumbraService.ModDirectory!.ToLowerInvariant(), "", StringComparison.OrdinalIgnoreCase);
        var entry = _fileCaches.SelectMany(v => v.Value).FirstOrDefault(f => f.ResolvedFilepath.EndsWith(cleanedPath, StringComparison.OrdinalIgnoreCase));

        if(entry == null)
        {
            Brio.Log.Debug("Found no entries for {path}", cleanedPath);
            return CreateFileEntry(path);
        }

        var validatedCacheEntry = GetValidatedFileCache(entry);

        return validatedCacheEntry;
    }
    public Dictionary<string, FileCacheEntity?> GetFileCachesByPaths(string[] paths)
    {
        _getCachesByPathsSemaphore.Wait();

        try
        {
            var cleanedPaths = paths.Distinct(StringComparer.OrdinalIgnoreCase).ToDictionary(p => p,
                p => p.Replace("/", "\\", StringComparison.OrdinalIgnoreCase)
                    .Replace(_penumbraService.ModDirectory!, _penumbraService.ModDirectory!.EndsWith('\\') ? PenumbraPrefix + '\\' : PenumbraPrefix, StringComparison.OrdinalIgnoreCase)
                    .Replace(CacheFolder, CacheFolder.EndsWith('\\') ? CachePrefix + '\\' : CachePrefix, StringComparison.OrdinalIgnoreCase)
                    .Replace("\\\\", "\\", StringComparison.Ordinal),
                StringComparer.OrdinalIgnoreCase);

            Dictionary<string, FileCacheEntity?> result = new(StringComparer.OrdinalIgnoreCase);

            var dict = _fileCaches.SelectMany(f => f.Value)
                .ToDictionary(d => d.PrefixedFilePath, d => d, StringComparer.OrdinalIgnoreCase);

            foreach(var entry in cleanedPaths)
            {
                //_logger.LogDebug("Checking {path}", entry.Value);

                if(dict.TryGetValue(entry.Value, out var entity))
                {
                    var validatedCache = GetValidatedFileCache(entity);
                    result.Add(entry.Key, validatedCache);
                }
                else
                {
                    if(!entry.Value.Contains(CachePrefix, StringComparison.Ordinal))
                        result.Add(entry.Key, CreateFileEntry(entry.Key));
                    else
                        result.Add(entry.Key, CreateCacheEntry(entry.Key));
                }
            }

            return result;
        }
        finally
        {
            _getCachesByPathsSemaphore.Release();
        }
    }

    private FileCacheEntity ReplacePathPrefixes(FileCacheEntity fileCache)
    {
        if(fileCache.PrefixedFilePath.StartsWith(PenumbraPrefix, StringComparison.OrdinalIgnoreCase))
        {
            fileCache.SetResolvedFilePath(fileCache.PrefixedFilePath.Replace(PenumbraPrefix, _penumbraService.ModDirectory, StringComparison.Ordinal));
        }
        else if(fileCache.PrefixedFilePath.StartsWith(CachePrefix, StringComparison.OrdinalIgnoreCase))
        {
            fileCache.SetResolvedFilePath(fileCache.PrefixedFilePath.Replace(CachePrefix, TempPath, StringComparison.Ordinal));
        }

        return fileCache;
    }
    private FileCacheEntity? GetValidatedFileCache(FileCacheEntity fileCache)
    {
        var resultingFileCache = ReplacePathPrefixes(fileCache);
        //Brio.Log.Debug("Validating {path}", fileCache.PrefixedFilePath);
        resultingFileCache = Validate(resultingFileCache);
        return resultingFileCache;
    }

    public void Dispose()
    {
        ClearTemp();

        GC.SuppressFinalize(this);
    }
}
