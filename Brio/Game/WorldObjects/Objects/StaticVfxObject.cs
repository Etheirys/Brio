using Brio.Core;
using Brio.Game.Core;
using Brio.Game.VFX.Intertop;
using Brio.Resources;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using System;
using System.Numerics;

namespace Brio.Game.WorldObjects.Objects;

public unsafe class StaticVfxObject : WorldObject
{
    private readonly VFXService _vFXService;

    public VfxObject* VFX;
    public DateTime Expires;

    public bool IsLooping = false;
    public bool WantsReload = false;
    public bool ShouldResume = true;
    public bool ShouldStartWithoutSpeed = false;

    public bool Moved = false;

    public bool NeedsRefresh => IsValid && DateTime.UtcNow >= Expires;
    public int VfxRefreshIntervalSeconds { get; set; } = 15;

    public override WorldObjectType ObjectType => WorldObjectType.StaticVfx;
    public override string FriendlyName { get; protected set; } = "VFX";

    public override string FriendlyPath
    {
        get
        {
            if(string.IsNullOrEmpty(field))
            {
                var info = GameDataProvider.Instance.PathDatabase.GetPathDataByPath(Path);
                if(info != null)
                    FriendlyName = info.Name;
                return field = System.IO.Path.GetFileNameWithoutExtension(Path);
            }
            else
                return field;
        }
        set;
    }

    public override nint Address => (nint)VFX;

    public VfxResourceInstance* VfxResource => (VfxResourceInstance*)VFX->VfxResourceInstance;

    public Vector3 Intensity => IsValid ? VfxResource->Intensity : Vector3.Zero;
    public float Speed => IsValid ? VfxResource->Speed : 0f;

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

        _vFXService.AddHandledVFX(Path);

        VFX->SomeFlags &= 0xF7;
        VFX->Update(0f);

        SetTransform(Transform);

        if(IsValid)
        {
            _vFXService.PlayStaticVFX(VFX);
            Expires = DateTime.UtcNow + TimeSpan.FromSeconds(VfxRefreshIntervalSeconds);

            IsVisible = true;
        }
    }

    public override void Recreate(string path)
    {
        Destroy();
        Path = path;
        PathMeta = null;
        FriendlyPath = string.Empty;
        VFX = null;
        Create();
    }

    public void Reload()
    {
        VFX = VfxObject.Create(Path, string.Empty);
        if(!IsValid) return;

        SetTransform(Transform);
        IsDirty = false;

        VFX->SomeFlags &= 0xF7;
        VFX->Update(0f);

        if(IsValid)
        {
            _vFXService.PlayStaticVFX(VFX);
            Expires = DateTime.UtcNow + TimeSpan.FromSeconds(VfxRefreshIntervalSeconds);

            IsVisible = true;
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


    public void Resume()
    {
        if(!IsValid) return;

        _vFXService.PlayStaticVFX(VFX);
    }

    public void Pause()
    {
        if(!IsValid) return;

        _vFXService.PauseVFX(VfxResource);
    }

    public bool IsActive()
    {
        if(!IsValid) return false;

        return _vFXService.IsActiveStatic(VFX);
    }

    public void SetSpeed(float speed)
    {
        if(!IsValid) return;

        _vFXService.SetVFXSpeed(VfxResource, speed);
    }

    public void SetIntensity(Vector3 intensity)
    {
        if(!IsValid) return;

        VfxResource->Intensity = intensity;

        VFX->NotifyTransformChanged();
        VFX->UpdateCulling();
    }

    public override Transform GetTransform()
    {
        if(!IsValid) return Transform.Identity;
        return new Transform { Position = VFX->Position, Rotation = VFX->Rotation, Scale = VFX->Scale };
    }
    public override void SetTransform(Transform transform)
    {
        Transform = transform;

        if(!IsValid) return;
        VFX->Position = transform.Position;
        VFX->Rotation = transform.Rotation;
        VFX->Scale = transform.Scale;

        VFX->NotifyTransformChanged();
        VFX->UpdateCulling();

        if(ShouldResume)
            Resume();

        Moved = true;
    }

    public override void Destroy()
    {
        VFX->CleanupRender();
        VFX->Dtor(1);

        // techically this is wrong as if you have more then one of the same VFX it still be loaded in engine.
        // But it's ok, we don't need to worry about this, just have the user rspawn the VFX if anything goes wrong
        _vFXService.RemoveHandledVFX(Path);
    }

    public override void Dispose()
    {
        if(IsValid)
            Destroy();

        VFX = null;

        GC.SuppressFinalize(this);
    }
}
