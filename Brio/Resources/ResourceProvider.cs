using Brio.Core;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Brio.Resources;

internal class ResourceProvider : IDisposable
{
    public static ResourceProvider Instance { get; private set; } = null!;

    private readonly Dictionary<string, object> _cachedDocuments = [];
    private readonly Dictionary<string, IDalamudTextureWrap> _cachedImages = [];

    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly ITextureProvider _textureProvider;

    public ResourceProvider(IDalamudPluginInterface pluginInterface, ITextureProvider textureProvider)
    {
        Instance = this;
        _pluginInterface = pluginInterface;
        _textureProvider = textureProvider;

        Localize.Load(this);
    }

    public T GetResourceDocument<T>(string name)
    {
        if(_cachedDocuments.TryGetValue(name, out var cached))
            return (T)cached;

        using var stream = GetRawResourceStream(name);
        using var reader = new StreamReader(stream);
        var txt = reader.ReadToEnd();
        var document = JsonSerializer.Deserialize<T>(txt);
        _cachedDocuments[name] = document!;
        return document;
    }

    public IDalamudTextureWrap GetResourceImage(string name)
    {
        if(_cachedImages.TryGetValue(name, out var cached))
            return cached;

        using var stream = GetRawResourceStream(name);
        using var reader = new BinaryReader(stream);
        var imgBin = reader.ReadBytes((int)stream.Length);
        var imgTask = _textureProvider.CreateFromImageAsync(imgBin);
        imgTask.Wait(); // TODO: Don't block
        var img = imgTask.Result;
        _cachedImages[name] = img;
        return img;
    }

    public Stream GetRawResourceStream(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"Brio.Resources.Embedded.{name}";
        var stream = assembly.GetManifestResourceStream(resourceName);
        return stream ?? throw new Exception($"Resource {name} not found.");
    }

    public string GetRawResourceString(string name)
    {
        using var stream = GetRawResourceStream(name);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public T GetFileDocument<T>(string path)
    {
        using var stream = GetFileStream(path);
        using var reader = new StreamReader(stream);
        var txt = reader.ReadToEnd();
        var document = JsonSerializer.Deserialize<T>(txt);
        return document;
    }

    public void SaveFileDocument<T>(string path, T doc)
        where T : notnull
    {
        var txt = JsonSerializer.Serialize(doc);
        File.WriteAllText(path, txt);
    }

    public Stream GetFileStream(string path) => File.OpenRead(path);

    public void Dispose()
    {
        foreach(var img in _cachedImages.Values)
            img?.Dispose();

        _cachedImages?.Clear();
        _cachedDocuments?.Clear();
    }
}
