using Brio.Core;
using Brio.Game.Core;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine.Group;
using System;

using CSLayoutTransform = FFXIVClientStructs.FFXIV.Client.LayoutEngine.Transform;

namespace Brio.Game.WorldObjects.Objects;

public unsafe class FurnitureObject : WorldObjectBase
{
    private readonly SGLService _sGLService;

    public SharedGroupLayoutInstance* SGL;

    public override WorldObjectType ObjectType => WorldObjectType.Furniture;
    public override string FriendlyName => "Furniture";
    public override nint Address => (nint)SGL;

    public override bool IsVisible
    {
        get => IsValid && field;
        set
        {
            if(IsValid)
                SGL->SetActive(field = value);
            IsDirty = true;
        }
    } = true;

    public FurnitureObject(string path, SGLService sGLService, Transform transform)
    {
        Path = path;
        _sGLService = sGLService;

        Transform = transform;
        OriginalTransform = Transform;

        Create();
    }

    private void Create()
    {
        var sglTransform = new CSLayoutTransform
        {
            Translation = Transform.Position,
            Rotation = Transform.Rotation,
            Scale = Transform.Scale
        };

        SGL = _sGLService.CreateSGL(Path, sglTransform);
        if(!IsValid) return;

        IsDirty = true;

        Brio.Log.Warning("Created SGL at address: " + ((nint)SGL).ToString("X"));

        _sGLService.SetupStains(SGL);

        SGL->SetTransformImpl(&sglTransform);
    }

    public override Transform GetTransform()
    {
        if(!IsValid) return Transform;

        var sglTransform = SGL->GetTransformImpl();
        return new Transform
        {
            Position = sglTransform->Translation,
            Rotation = sglTransform->Rotation,
            Scale = sglTransform->Scale
        };
    }
    public override void SetTransform(Transform transform)
    {
        Transform = transform;
        if(!IsValid) return;

        IsDirty = true;

        var sglTransform = new CSLayoutTransform
        {
            Translation = transform.Position,
            Rotation = transform.Rotation,
            Scale = transform.Scale
        };

        SGL->SetTransformImpl(&sglTransform);
        SGL->Instances.ApplyTransforms();
    }

    public override void Destroy()
    {
        SGL->Deinit();
        _sGLService.DestroySGL(SGL);
        //SGL->Dtor(1);
    }

    public override void Dispose()
    {
        if(IsValid)
            Destroy();

        SGL = null;

        GC.SuppressFinalize(this);
    }
}
