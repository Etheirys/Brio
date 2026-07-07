using Brio.Capabilities.Core;
using Brio.Entities;
using Brio.Services;
using Brio.UI.Widgets.ReferenceImage;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Plugin.Services;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Brio.Capabilities.ReferenceImage;

public class ReferenceImageCapability : Capability
{
    private readonly ReferenceImageService _service;
    private IDalamudTextureWrap? _texture;

    public IDalamudTextureWrap? Texture => _texture;
    public ReferenceImageEntity ReferenceImageEntity { get; }

    public ReferenceImageCapability(ReferenceImageEntity parent, ReferenceImageService service, ITextureProvider textureProvider) : base(parent)
    {
        _service = service;

        ReferenceImageEntity = parent;

        Widget = new ReferenceImageWidget(this);

        Task.Run(() => LoadImage(textureProvider));
    }

    // TODO (ken) I should really make this work in the real load image method
    private async void LoadImage(ITextureProvider textureProvider)
    {
        try
        {
            var bytes = await File.ReadAllBytesAsync(ReferenceImageEntity.Path);
            _texture = await textureProvider.CreateFromImageAsync(bytes);
        }
        catch(Exception ex)
        {
            Brio.Log.Error(ex, $"Failed to load reference image: {ReferenceImageEntity.Path}");
        }
    }

    public void Destroy()
        => _service.Destroy(ReferenceImageEntity);

    public override void Dispose()
    {
        _texture?.Dispose();
        base.Dispose();
    }
}
