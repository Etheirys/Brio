using Brio.Core;
using Brio.Game.Core;
using Brio.Game.VFX.Intertop;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using System;

namespace Brio.Game.WorldObjects.Objects;

public unsafe class StaticVfxObject : WorldObjectBase
{
    private readonly VFXService _vFXService;

    public VfxObject* VFX;
    public DateTime Expires;

    public bool IsLooping = true;
    private const int VfxRefreshSeconds = 5;
    public bool NeedsRefresh => IsValid && DateTime.UtcNow >= Expires;

    public override WorldObjectType ObjectType => WorldObjectType.StaticVfx;
    public override string FriendlyName => "VFX";
    public override nint Address => (nint)VFX;

    public override bool IsVisible 
    {
        get => IsValid && ((VFXObject*)VFX)->Alpha > 0f;
        set
        {
            if(IsValid)
                ((VFXObject*)VFX)->Alpha = value ? 1f : 0f;
        }
    }

    public StaticVfxObject(string path, VFXService vFXService, Transform transform)
    {
        Path = path;
        _vFXService = vFXService;

        Transform = transform;
        OriginalTransform = Transform;

        Create();
    }

    private void Create()
    {
        VFX = VfxObject.Create(Path, string.Empty);
        if(!IsValid) return;

        VFX->SomeFlags &= 0xF7;
        VFX->Update(0f);

        SetTransform(Transform);

        if(IsValid)
        {
            _vFXService.RunStaticVFX(VFX);
            Expires = DateTime.UtcNow + TimeSpan.FromSeconds(VfxRefreshSeconds);
        
            IsVisible = true;
        }
    }

    public void Reload()
    {
        VFX = VfxObject.Create(Path, string.Empty);
        VFX->SomeFlags &= 0xF7;
        VFX->Update(0f);

        SetTransform(Transform);

        if(IsValid)
        {
            _vFXService.RunStaticVFX(VFX);
            Expires = DateTime.UtcNow + TimeSpan.FromSeconds(VfxRefreshSeconds);
        }
    }

    public void CheckRefresh()
    {
        if(!NeedsRefresh || !IsLooping)
            return;

        if(IsValid)
            Destroy();

        Reload();
    }

    public override Transform GetTransform()
    {
        if(!IsValid) return Transform.Identity;
        return new Transform { Position = VFX->Position, Rotation = VFX->Rotation, Scale = VFX->Scale };
    }
    public override void SetTransform(Transform transform)
    {
        if(transform.Position != Transform.Position)
            IsDirty = true;
        Transform = transform;

        if(!IsValid) return;
        VFX->Position = transform.Position;
        VFX->Rotation = transform.Rotation;
        VFX->Scale = transform.Scale;

        IsVisible = false;

        VFX->NotifyTransformChanged();
        VFX->UpdateTransforms(false);
    }

    public override void Destroy()
    {
        VFX->CleanupRender();
        VFX->Dtor(1);
    }

    public override void Dispose()
    {
        if(IsValid)
            Destroy();

        VFX = null;

        GC.SuppressFinalize(this);
    }
}
