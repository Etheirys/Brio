using Brio.Resources;
using Brio.Resources.Extra;
using Dalamud.Plugin;
using Swan;
using System;
using System.IO;
using System.IO.Compression;

namespace Brio.Services;

public enum PathTarget
{
    User,
    Plugin,
}

public class PathMetadataService
{
    private readonly PathDatabase _db;
    private readonly string _userFile;
    private readonly string _pluginFile;

    public PathStore UserStore => _db.UserStore;
    public PathStore PluginStore => _db.PluginStore;

    public PathDatabase PathDatabase => _db;

    public PathMetadataService(GameDataProvider gameData, IDalamudPluginInterface pluginInterface, ResourceProvider resourceProvider)
    {
        _db = gameData.PathDatabase;

        var dataDir = Path.Combine(pluginInterface.GetPluginConfigDirectory(), "Data");
        Directory.CreateDirectory(dataDir);

        _userFile = Path.Combine(dataDir, "PathStore.user.bpath");
        //_pluginFile = Path.Combine(dataDir, "PathStore.plugin.bpath");

        byte[] userBytes = [];
        if(File.Exists(_userFile))
            userBytes = File.ReadAllBytes(_userFile);

        byte[] pluginBytes = [];

        //if(File.Exists(_pluginFile))
        //    pluginBytes = File.ReadAllBytes(_pluginFile);

        _pluginFile = string.Empty;

        using var pathStream = resourceProvider.GetRawResourceStream("Data.PathStore.bpath");
        using(var ms = new MemoryStream())
        {
            pathStream.CopyTo(ms);
            pluginBytes = ms.ToArray();
        }

        Load(PathTarget.User, ref userBytes);
        Load(PathTarget.Plugin, ref pluginBytes);
    }

    //

    public PathStore StoreFor(PathTarget target)
        => target == PathTarget.Plugin ? _db.PluginStore : _db.UserStore;
    private string FileFor(PathTarget target)
        => target == PathTarget.Plugin ? _pluginFile : _userFile;

    //

    public void Set(PathTarget target, string path, PathData meta)
    {
        _db.SetMetaData(StoreFor(target), path, meta);
        Save(target);
    }
    public bool Remove(PathTarget target, string path)
    {
        var removed = _db.RemoveMetaData(StoreFor(target), path);
        if(removed)
            Save(target);
        return removed;
    }

    public void Save(PathTarget target)
    {
        try
        {
            PathDatabase.Save(FileFor(target), StoreFor(target), PathStore.Magic, PathStore.FormatVersion);
        }
        catch(Exception ex)
        {
            Brio.Log.Error(ex, "Failed to save path metadata store");
        }
    }
    private void Load(PathTarget target, ref byte[] data)
    {
        try
        {
            var loaded = PathDatabase.Deserialize<PathStore>(data, PathStore.Magic);
            var store = StoreFor(target);

            store.KeyVersion = loaded.KeyVersion;
            store.Entries.Clear();
            foreach(var entry in loaded.Entries)
                store.Entries[entry.Key] = entry.Value;
        }
        catch(Exception ex)
        {
            Brio.Log.Error(ex, $"Failed to load data for store {target}");
        }
    }

    public void Export(PathTarget target, string file)
    {
        try
        {
            var export = new PathMetaExport
            {
                ExportedUTC = DateTime.UtcNow.Ticks,
                DB = StoreFor(target),
            };
            File.WriteAllBytes(file, PathDatabase.Serialize(export, PathMetaExport.Magic, PathMetaExport.FormatVersion));
        }
        catch(Exception ex)
        {
            Brio.Log.Error(ex, "Failed to export path metadata store");
        }
    }
    public MergeResult Import(PathTarget target, string file, PathDatabase.ConflictPolicy policy = PathDatabase.ConflictPolicy.LastWriteWins)
    {
        var import = PathDatabase.Deserialize<PathMetaExport>(File.ReadAllBytes(file), PathMetaExport.Magic);
        var result = PathDatabase.MergeStore(StoreFor(target), import, policy);
        Save(target);
        return result;
    }
}
