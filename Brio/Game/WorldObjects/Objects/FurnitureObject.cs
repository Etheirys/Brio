using Brio.Core;
using Brio.Game.Core;
using Brio.Resources;
using FFXIVClientStructs.FFXIV.Client.Graphics;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine.Group;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine.Node;
using System;
using System.Numerics;
using BrioTransform = Brio.Core.Transform;
using CSLayoutTransform = FFXIVClientStructs.FFXIV.Client.LayoutEngine.Transform;
using CSSceneObject = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.Object;

namespace Brio.Game.WorldObjects.Objects;

public unsafe class FurnitureObject : WorldObject
{
    private const int IsReadyVTable = 22;
    private const byte DefaultStainID = 0;

    private readonly SGLService _sGLService;

    private delegate* unmanaged<SharedGroupLayoutInstance*, byte> _isReady;
    public SharedGroupLayoutInstance* SGL;

    public bool VsualStateDirty = true;

    public float Transparency { get; private set; }
    public byte StainID { get; private set; } = DefaultStainID;

    public bool IsCustomColor { get; private set; } = false;
    public Vector4 CustomColor { get; private set; } = Vector4.One;

    public override WorldObjectType ObjectType => WorldObjectType.Furniture;
    public override string FriendlyName { get; protected set; } = string.Empty;
    public override string FriendlyPath
    {
        get
        {
            if(string.IsNullOrEmpty(field))
            {
                var furnitureInfo = GameDataProvider.Instance.FurnitureDatabase.GetByPath(Path);
                if(furnitureInfo != null)
                    FriendlyName = furnitureInfo.Name;
                return field = System.IO.Path.GetFileNameWithoutExtension(Path);
            }
            else
                return field;
        }
    }
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

    public FurnitureObject(string path, SGLService sGLService, BrioTransform transform)
    {
        Path = path;
        _sGLService = sGLService;

        Transform = transform;
        OriginalTransform = Transform;

        Create();
    }

    private void Create()
    {
        var sglTransform = ToLayoutTransform(Transform);

        SGL = _sGLService.CreateSGL(Path, sglTransform);
        if(!IsValid)
            return;

        IsDirty = true;

        Brio.Log.Debug("Created SGL at address: " + ((nint)SGL).ToString("X"));

        var vtable = *(nint**)SGL;
        if(vtable != null)
            _isReady = (delegate* unmanaged<SharedGroupLayoutInstance*, byte>)vtable[IsReadyVTable];

        _sGLService.SetupStains(SGL);

        SGL->SetTransformImpl(&sglTransform);

        VsualStateDirty = true;

        ClearColor();
    }

    public override void Recreate(string path)
    {
        Destroy();
        Path = path;
        SGL = null;
        Create();
    }

    public bool IsReady()
    {
        if(SGL == null)
            return false;

        return _isReady != null && _isReady(SGL) != 0;
    }

    public void SetTransparency(float transparency)
    {
        Transparency = Math.Clamp(transparency, 0f, 1f);
        UpdateVisualState();
    }

    public void SetStain(byte stainId)
    {
        StainID = stainId;
        IsCustomColor = false;

        UpdateVisualState();
    }

    public void SetCustomColor(Vector4 color)
    {
        CustomColor = color.ToOpaqueNormalizedColor();
        IsCustomColor = true;

        UpdateVisualState();
    }

    public void ClearColor()
    {
        StainID = DefaultStainID;
        IsCustomColor = false;
        CustomColor = Vector4.One;

        UpdateVisualState();
    }

    public bool UpdateVisualState()
    {
        VsualStateDirty = true;

        if(SGL == null || !IsReady())
            return false;

        if(!TryApplyTransparency())
            return false;

        if(!TryApplyColor())
            return false;

        VsualStateDirty = false;
        return true;

        bool TryApplyTransparency()
        {
            if(!HasChildInstances(SGL))
                return false;

            ApplyToChildInstances(&SGL->Instances, Transparency, null, applyTransparency: true);
            return true;
        }

        bool TryApplyColor()
        {
            if(IsCustomColor)
            {
                if(!HasChildInstances(SGL))
                    return false;

                var color = CustomColor.ToByteColor();
                ApplyToChildInstances(&SGL->Instances, 0f, &color, applyStain: true);
                return true;
            }

            if(SGL->StainInfo != null && SGL->TryApplyStain(StainID))
                return true;

            if(!GetNativeStainColor(StainID, out var sColor))
                return false;

            ApplyToChildInstances(&SGL->Instances, 0f, &sColor, applyStain: true);
            return true;
        }

        bool GetNativeStainColor(byte chosenStainID, out ByteColor stainColor)
        {
            stainColor = default;
            if(chosenStainID == 0)
            {
                var stainInfo = SGL->StainInfo;
                if(stainInfo == null || stainInfo->DefaultStainIndex == 0)
                    return false;

                chosenStainID = stainInfo->DefaultStainIndex;
            }

            var nativeColor = SharedGroupLayoutInstance.GetObjectStainColorByIndex(chosenStainID);
            if(nativeColor == null)
                return false;

            stainColor = *nativeColor;
            return true;
        }

        static void ApplyToChildInstances(ChildNodeContainer* container, float transparency, ByteColor* stainColor, bool applyTransparency = false, bool applyStain = false)
        {
            foreach(var child in container->Instances)
            {
                var node = child.Value;
                if(node == null || node->Instance == null)
                    continue;

                if(applyTransparency)
                {
                    var primaryGraphics = node->Instance->GetGraphics();
                    ApplyTransparency(primaryGraphics, transparency);

                    var secondaryGraphics = node->Instance->GetGraphics2();
                    if(secondaryGraphics != null && secondaryGraphics != primaryGraphics)
                        ApplyTransparency(secondaryGraphics, transparency);
                }

                if(applyStain)
                    node->Instance->ApplyStain(stainColor);

                if(node->Instance->Id.Type == InstanceType.SharedGroup)
                {
                    var childGroup = (SharedGroupLayoutInstance*)node->Instance;
                    ApplyToChildInstances(&childGroup->Instances, transparency, stainColor, applyTransparency, applyStain);
                }
            }
        }

        static void ApplyTransparency(CSSceneObject* graphics, float transparency)
        {
            var drawObject = (DrawObject*)graphics;
            if(drawObject == null)
                return;

            drawObject->SetTransparency(transparency);
            drawObject->UpdateMaterials();
            drawObject->UpdateCulling();
        }
    }

    private static bool HasChildInstances(SharedGroupLayoutInstance* sgl)
        => (nint)sgl->Instances.Instances.First != (nint)sgl->Instances.Instances.Last;

    public override BrioTransform GetTransform()
    {
        if(!IsValid)
            return Transform;

        return ToBrioTransform(*SGL->GetTransformImpl());
    }

    public override void SetTransform(BrioTransform transform)
    {
        Transform = transform;
        if(!IsValid)
            return;

        IsDirty = true;

        var sglTransform = ToLayoutTransform(transform);

        SGL->SetTransformImpl(&sglTransform);
        SGL->Instances.ApplyTransforms();
    }

    private static BrioTransform ToBrioTransform(CSLayoutTransform transform) => new()
    {
        Position = transform.Translation,
        Rotation = transform.Rotation,
        Scale = transform.Scale
    };
    private static CSLayoutTransform ToLayoutTransform(BrioTransform transform) => new()
    {
        Translation = transform.Position,
        Rotation = transform.Rotation,
        Scale = transform.Scale
    };

    public override void Destroy()
    {
        SGL->Deinit();
        _sGLService.DestroySGL(SGL);
    }

    public override void Dispose()
    {
        if(IsValid)
            Destroy();

        SGL = null;

        GC.SuppressFinalize(this);
    }
}
