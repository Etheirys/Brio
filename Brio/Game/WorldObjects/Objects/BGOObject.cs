using Brio.Core;
using Brio.Resources;
using Brio.Resources.Extra;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using System;

using BrioBgObjectEx = Brio.Game.WorldObjects.Interop.BgObjectEx;

namespace Brio.Game.WorldObjects.Objects;

// this name makes me giggle
public unsafe class BGOObject : WorldObject
{
    public BrioBgObjectEx* BgObject;

    public override WorldObjectType ObjectType => WorldObjectType.BgObject;
    public override string FriendlyName { get; protected set; } = "World Object";
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

    public override nint Address => (nint)BgObject;

    public override bool IsVisible
    {
        get => IsValid && BgObject->Transparency == 0;
        set
        {
            if(IsValid)
                BgObject->Transparency = value ? (byte)0 : (byte)255;
        }
    }

    public BGOObject(string path, Transform transform)
    {
        Path = path;

        Transform = transform;
        OriginalTransform = Transform;

        Create(path);
    }

    private void Create(string path)
    {
        BgObject = BrioBgObjectEx.Create(path);
        if(!IsValid) return;

        SetTransform(Transform);

        BgObject->UpdateCulling();
    }

    public override void Recreate(string path)
    {
        Destroy();
        Path = path;
        PathMeta = null;
        FriendlyPath = string.Empty;
        BgObject = null;
        Create(path);
    }

    public override Transform GetTransform()
    {
        if(!IsValid) return Transform.Identity;
        var bg = (BgObject*)BgObject;
        return new Transform { Position = bg->Position, Rotation = bg->Rotation, Scale = bg->Scale };
    }
    public override void SetTransform(Transform transform)
    {
        Brio.Log.Warning($"Setting transform for {FriendlyName} at address {Address:X} to Position: {transform.Position}, Rotation: {transform.Rotation}, Scale: {transform.Scale}");
       
        Transform = transform;

        if(!IsValid) return;
        var bg = (BgObject*)BgObject;
        bg->Position = transform.Position;
        bg->Rotation = transform.Rotation;
        bg->Scale = transform.Scale;

        BgObject->UpdateCulling();
        BgObject->UpdateTransforms();
    }

    public override void Destroy()
    {
        if(!IsValid) return;

        // This is done this way because, well I am not sure. My head hurts,
        // has to be done this way or boom, dont know what I did wrong
        ((BgObject*)Address)->CleanupRender();
        ((BgObject*)Address)->Dtor(1);
    }

    public override void Dispose()
    {
        if(IsValid)
            Destroy();

        BgObject = null;

        GC.SuppressFinalize(this);
    }

}
