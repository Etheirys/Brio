using Brio.Config;
using Brio.Entities;
using Brio.Game.Actor.Appearance;
using Brio.Game.Actor.Extensions;
using Brio.Game.Camera;
using Brio.Game.GPose;
using Brio.IPC;
using Dalamud.Game;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using InteropGenerator.Runtime;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static Brio.Game.Actor.ActorRedrawService;
using DrawDataContainer = FFXIVClientStructs.FFXIV.Client.Game.Character.DrawDataContainer;

namespace Brio.Game.Actor;

public class ActorAppearanceService : IDisposable
{
    private readonly GPoseService _gPoseService;
    private readonly ConfigurationService _configurationService;
    private readonly ActorRedrawService _redrawService;
    private readonly GlamourerService _glamourerService;
    private readonly VirtualCameraManager _virtualCameraManager;
    private readonly EntityManager _entityManager;
    private readonly IObjectTable _objectTable;

    private delegate byte EnforceKindRestrictionsDelegate(nint a1, nint a2);
    private readonly Hook<EnforceKindRestrictionsDelegate> _enforceKindRestrictionsHook = null!;

    private delegate nint UpdateWetnessDelegate(nint a1);
    private readonly Hook<UpdateWetnessDelegate> _updateWetnessHook = null!;

    private unsafe delegate nint UpdateTintDelegate(nint charaBase, nint tint);
    private readonly Hook<UpdateTintDelegate> _updateTintHook = null!;

    private unsafe delegate* unmanaged<DrawDataContainer*, byte, ushort, void> _setFacewear;
    private unsafe delegate* unmanaged<CharacterLookAtController*, LookAtTarget*, uint, nint, void> _updateLookAt;

    public bool CanTint => _configurationService.Configuration.Appearance.EnableTinting;

    public unsafe ActorAppearanceService(GPoseService gPoseService, VirtualCameraManager virtualCameraManager, IObjectTable objectTable, ConfigurationService configurationService, ActorRedrawService redrawService, GlamourerService glamourerService, EntityManager entityManager, ISigScanner sigScanner, IGameInteropProvider hooks)
    {
        _gPoseService = gPoseService;
        _configurationService = configurationService;
        _redrawService = redrawService;
        _glamourerService = glamourerService;
        _entityManager = entityManager;
        _objectTable = objectTable;
        _virtualCameraManager = virtualCameraManager;

        var enforceKindRestrictionsAddress = sigScanner.ScanText("E8 ?? ?? ?? ?? 41 B0 ?? 48 8B D6 48 8B");
        _enforceKindRestrictionsHook = hooks.HookFromAddress<EnforceKindRestrictionsDelegate>(enforceKindRestrictionsAddress, EnforceKindRestrictionsDetour);
        _enforceKindRestrictionsHook.Enable();

        var updateWetnessAddress = sigScanner.ScanText("40 53 48 83 EC ?? 48 8B 01 48 8B D9 FF 90 ?? ?? ?? ?? 48 85 C0 74 ?? 48 8B 03 48 8B CB 48 83 C4");
        _updateWetnessHook = hooks.HookFromAddress<UpdateWetnessDelegate>(updateWetnessAddress, UpdateWetnessDetour);
        _updateWetnessHook.Enable();

        var updateTintHookAddress = Marshal.ReadInt64((nint)(CharacterBase.StaticVirtualTablePointer) + 0xC0);
        _updateTintHook = hooks.HookFromAddress<UpdateTintDelegate>((nint)updateTintHookAddress, UpdateTintDetour);
        _updateTintHook.Enable();

        var setFacewearAddress = sigScanner.ScanText("E8 ?? ?? ?? ?? FF C3 48 8D ?? ?? ?? ?? ?? ?? ?? 0F");
        _setFacewear = (delegate* unmanaged<DrawDataContainer*, byte, ushort, void>)setFacewearAddress;

        var updateFaceTrackerAddress = sigScanner.ScanText("E8 ?? ?? ?? ?? 8B D7 48 8B CB E8 ?? ?? ?? ?? 41 ?? ?? 8B D7 48 ?? ?? 48 ?? ?? ?? ?? 48 83 ?? ?? 5F");
        _updateLookAt = (delegate* unmanaged<CharacterLookAtController*, LookAtTarget*, uint, nint, void>)updateFaceTrackerAddress;

        var actorLookAtLoopAddress = sigScanner.ScanText("E8 ?? ?? ?? ?? 48 83 C3 08 48 83 EF 01 75 CF 48 ?? ?? ?? ?? 48");
        _actorLookAtLoop = hooks.HookFromAddress<ActorLookAtLoopDelegate>(actorLookAtLoopAddress, ActorLookAtLoopDetour);
        _actorLookAtLoop.Enable();
    }

    public delegate nint ActorLookAtLoopDelegate(nint a1);
    public Hook<ActorLookAtLoopDelegate> _actorLookAtLoop = null!;

    Dictionary<ulong, LookAtDataHolder> _lookAtHandles = [];

    public unsafe IntPtr ActorLookAtLoopDetour(nint a1)
    {
        if(_gPoseService.IsGPosing)
        {
            var lookAtContainer = (ContainerInterface*)a1;
            var obj = _objectTable.CreateObjectReference((nint)lookAtContainer->OwnerObject);
            if(obj is not null && obj.IsValid() && obj.IsGPose() && _lookAtHandles.ContainsKey(obj.GameObjectId))
            {
                actorLook(obj, _lookAtHandles[obj.GameObjectId]);
            }
        }

        return _actorLookAtLoop.Original(a1);
    }

    public unsafe void actorLook(IGameObject targetActor, LookAtDataHolder lookAtDataHolder)
    {
        LookAtSource lookAt = lookAtDataHolder.Target;

        lookAt.Eyes.LookAtTarget.LookMode = (uint)lookAtDataHolder.LookAtMode;
        lookAt.Head.LookAtTarget.LookMode = (uint)lookAtDataHolder.LookAtMode;
        lookAt.Body.LookAtTarget.LookMode = (uint)lookAtDataHolder.LookAtMode;

        if(lookAtDataHolder.LookatType == LookAtTargetMode.Camera)
        {
            var camera = _virtualCameraManager?.CurrentCamera;

            if(camera is null)
                return;

            if(lookAtDataHolder.lookAtTargetType.HasFlag(LookAtTargetType.Eyes))
                lookAt.Eyes.LookAtTarget.Position = camera.RealPosition;
            if(lookAtDataHolder.lookAtTargetType.HasFlag(LookAtTargetType.Body))
                lookAt.Head.LookAtTarget.Position = camera.RealPosition;
            if(lookAtDataHolder.lookAtTargetType.HasFlag(LookAtTargetType.Head))
                lookAt.Body.LookAtTarget.Position = camera.RealPosition;
        }
        else if(lookAtDataHolder.LookatType == LookAtTargetMode.None)
            return;

        var lookAtController = &((Character*)targetActor.Address)->LookAt.Controller;

        if(lookAtDataHolder.lookAtTargetType.HasFlag(LookAtTargetType.Eyes))
            _updateLookAt(lookAtController, &lookAt.Eyes.LookAtTarget, (uint)LookEditType.Eyes, 0);
        if(lookAtDataHolder.lookAtTargetType.HasFlag(LookAtTargetType.Body))
            _updateLookAt(lookAtController, &lookAt.Body.LookAtTarget, (uint)LookEditType.Body, 0);
        if(lookAtDataHolder.lookAtTargetType.HasFlag(LookAtTargetType.Head))
            _updateLookAt(lookAtController, &lookAt.Head.LookAtTarget, (uint)LookEditType.Head, 0);
    }

    public unsafe void TESTactorlookClear(IGameObject gameobj)
    {
        if(_lookAtHandles.TryGetValue(gameobj.GameObjectId, out var obj))
        {
            obj.LookAtMode = LookMode.None;
            obj.lookAtTargetType = LookAtTargetType.All;
            obj.LookatType = LookAtTargetMode.None;
        }
        else
        {

        }
    }

    public unsafe void TESTactorlook(IGameObject gameobj)
    {
        var camera = _virtualCameraManager?.CurrentCamera;

        if(camera is null)
            return;

        _lookAtHandles.TryGetValue(gameobj.GameObjectId, out LookAtDataHolder? obj);

        if(obj is null)
        {
            obj = new LookAtDataHolder();
            _lookAtHandles.Add(gameobj.GameObjectId, obj);
        }

        obj.LookAtMode = LookMode.Position;
        obj.lookAtTargetType = LookAtTargetType.All;
        obj.LookatType = LookAtTargetMode.Camera;
        obj.Target = new()
        {
            Body = new LookAtType
            {
                LookAtTarget = new LookAtTarget
                {
                    LookMode = (uint)LookMode.Position,
                    Position = camera.RealPosition
                }
            },
            Eyes = new LookAtType
            {
                LookAtTarget = new LookAtTarget
                {
                    LookMode = (uint)LookMode.Position,
                    Position = camera.RealPosition
                }
            },
            Head = new LookAtType
            {
                LookAtTarget = new LookAtTarget
                {
                    LookMode = (uint)LookMode.Position,
                    Position = camera.RealPosition
                }
            },
        };
    }
    public void RemoveFromLook(IGameObject gameobj)
    {
        if(_lookAtHandles.ContainsKey(gameobj.GameObjectId))
        {
            _lookAtHandles.Remove(gameobj.GameObjectId);
        }
    }

    //

    public async Task<RedrawResult> Redraw(ICharacter character)
    {
        var appearance = GetActorAppearance(character);
        return await SetCharacterAppearance(character, appearance, AppearanceImportOptions.All, true);
    }

    public async Task<RedrawResult> SetCharacterAppearance(ICharacter character, ActorAppearance appearance, AppearanceImportOptions options, bool forceRedraw = false)
    {
        var existingAppearance = GetActorAppearance(character);

        AppearanceSanitizer.SanitizeAppearance(ref appearance, existingAppearance);

        bool needsRedraw = forceRedraw;
        bool forceHeadToggles = false;
        bool glamourerReset = false;
        bool glamourerUnlocked = false;

        unsafe
        {
            var native = character.Native();

            // Model
            if(native->ModelContainer.ModelCharaId != appearance.ModelCharaId)
            {
                native->ModelContainer.ModelCharaId = appearance.ModelCharaId;
                needsRedraw |= true;
                glamourerReset |= true;
            }

            if(options.HasFlag(AppearanceImportOptions.Equipment) || options.HasFlag(AppearanceImportOptions.Customize))
            {
                // Hat toggle hack
                if(!existingAppearance.Equipment.Head.Equals(appearance.Equipment.Head))
                {
                    appearance.Runtime.IsHatHidden = false;
                    forceHeadToggles = true;
                }

                // Customize & Equipment
                if(!existingAppearance.Customize.Equals(appearance.Customize) || !existingAppearance.Equipment.Equals(appearance.Equipment))
                {
                    forceHeadToggles = true;

                    if(!existingAppearance.Customize.Equals(appearance.Customize))
                        glamourerReset |= true;

                    if(_glamourerService.CheckForLock(character))
                    {
                        if(!existingAppearance.Customize.Equals(appearance.Customize) || !existingAppearance.Equipment.Equals(appearance.Equipment))
                            glamourerUnlocked |= true;
                    }

                    if
                    (
                        existingAppearance.Customize.Race != appearance.Customize.Race ||
                        existingAppearance.Customize.Tribe != appearance.Customize.Tribe ||
                        existingAppearance.Customize.Gender != appearance.Customize.Gender ||
                        existingAppearance.Customize.BodyType != appearance.Customize.BodyType ||
                        existingAppearance.Customize.FaceType != appearance.Customize.FaceType
                    )
                        needsRedraw |= true;

                    if(!needsRedraw)
                    {
                        // Model redraw optimized if we can
                        var human = character.GetHuman();
                        if(human != null)
                        {

                            byte[] data = new byte[108];
                            fixed(byte* ptr = data)
                            {
                                if(options.HasFlag(AppearanceImportOptions.Customize))
                                {
                                    Buffer.MemoryCopy(appearance.Customize.Data, ptr, 32, 32);
                                }
                                else
                                {
                                    Buffer.MemoryCopy(existingAppearance.Customize.Data, ptr, 28, 28);
                                }

                                if(options.HasFlag(AppearanceImportOptions.Equipment))
                                {
                                    Buffer.MemoryCopy(appearance.Equipment.Data, ptr + 32, 80, 80);
                                }
                                else
                                {
                                    Buffer.MemoryCopy(existingAppearance.Equipment.Data, ptr + 32, 80, 80);
                                }

                                var didUpdate = human->Human.UpdateDrawData(ptr, false);
                                needsRedraw |= !didUpdate;
                            }
                        }
                        else
                        {
                            needsRedraw |= true;
                        }
                    }

                    if(options.HasFlag(AppearanceImportOptions.Customize))
                    {
                        // We can just set the data again incase we didn't earlier
                        *(ActorCustomize*)&native->DrawData.CustomizeData = appearance.Customize;
                    }

                    if(options.HasFlag(AppearanceImportOptions.Equipment))
                    {
                        fixed(EquipmentModelId* ptr = native->DrawData.EquipmentModelIds)
                        {
                            *(ActorEquipment*)ptr = appearance.Equipment;
                        }
                    }
                }

                // Facewear
                if(existingAppearance.Facewear != appearance.Facewear)
                {
                    if(needsRedraw)
                    {
                        character.BrioDrawData()->Facewear = appearance.Facewear;
                    }
                    else
                    {
                        _setFacewear(&native->DrawData, 0, appearance.Facewear);
                    }
                }
            }

            if(options.HasFlag(AppearanceImportOptions.Weapon))
            {
                // Weapons
                if(!needsRedraw)
                {

                    if(!existingAppearance.Weapons.MainHand.Equals(appearance.Weapons.MainHand))
                        native->DrawData.LoadWeapon(DrawDataContainer.WeaponSlot.MainHand, appearance.Weapons.MainHand, 0, 0, 0, 0);

                    if(!existingAppearance.Weapons.OffHand.Equals(appearance.Weapons.OffHand))
                        native->DrawData.LoadWeapon(DrawDataContainer.WeaponSlot.OffHand, appearance.Weapons.OffHand, 0, 0, 0, 0);
                }

                native->DrawData.Weapon(DrawDataContainer.WeaponSlot.MainHand).ModelId = appearance.Weapons.MainHand;
                native->DrawData.Weapon(DrawDataContainer.WeaponSlot.OffHand).ModelId = appearance.Weapons.OffHand;

                // Weapon Visibility
                if(existingAppearance.Runtime.IsMainHandHidden != appearance.Runtime.IsMainHandHidden)
                    character.GetWeaponDrawObjectData(ActorEquipSlot.MainHand)->IsHidden = appearance.Runtime.IsMainHandHidden;

                if(existingAppearance.Runtime.IsOffHandHidden != appearance.Runtime.IsOffHandHidden)
                    character.GetWeaponDrawObjectData(ActorEquipSlot.OffHand)->IsHidden = appearance.Runtime.IsOffHandHidden;

                if(existingAppearance.Runtime.IsPropHandHidden != appearance.Runtime.IsPropHandHidden)
                    character.GetWeaponDrawObjectData(ActorEquipSlot.Prop)->IsHidden = appearance.Runtime.IsPropHandHidden;
            }
        }


        if(glamourerUnlocked)
        {
            await _glamourerService.UnlockAndRevertCharacter(character);

            needsRedraw = true;
        }

        RedrawResult redrawResult = RedrawResult.Optmized;

        if(needsRedraw)
            redrawResult = await _redrawService.RedrawActor(character);

        if(glamourerReset)
            await _glamourerService.RevertCharacter(character);

        unsafe
        {

            var native = character.Native();

            existingAppearance = GetActorAppearance(character);

            if(options.HasFlag(AppearanceImportOptions.ExtendedAppearance))
            {
                // Hat
                if(existingAppearance.Runtime.IsHatHidden != appearance.Runtime.IsHatHidden || forceHeadToggles)
                {
                    native->DrawData.IsHatHidden = !appearance.Runtime.IsHatHidden;
                    native->DrawData.HideHeadgear(0, appearance.Runtime.IsHatHidden);
                }

                // Visor
                if(existingAppearance.Runtime.IsVisorToggled != appearance.Runtime.IsVisorToggled || forceHeadToggles)
                {
                    native->DrawData.SetVisor(appearance.Runtime.IsVisorToggled);
                    native->DrawData.IsVisorToggled = appearance.Runtime.IsVisorToggled;
                }

                // Wetness
                if(existingAppearance.ExtendedAppearance.Wetness != appearance.ExtendedAppearance.Wetness)
                {
                    var charaBase = character.GetCharacterBase();
                    charaBase->Wetness = appearance.ExtendedAppearance.Wetness;
                }
                if(existingAppearance.ExtendedAppearance.WetnessDepth != appearance.ExtendedAppearance.WetnessDepth)
                {
                    var charaBase = character.GetCharacterBase();
                    charaBase->WetnessDepth = appearance.ExtendedAppearance.WetnessDepth;
                }

                // Tints
                if(existingAppearance.ExtendedAppearance.CharacterTint != appearance.ExtendedAppearance.CharacterTint)
                {
                    var charaBase = character.GetCharacterBase();
                    charaBase->Tint = appearance.ExtendedAppearance.CharacterTint;
                }

                if(existingAppearance.ExtendedAppearance.MainHandTint != appearance.ExtendedAppearance.MainHandTint)
                {
                    var weaponCharaBase = character.GetWeaponCharacterBase(ActorEquipSlot.MainHand);
                    if(weaponCharaBase != null)
                        weaponCharaBase->Tint = appearance.ExtendedAppearance.MainHandTint;
                }

                if(existingAppearance.ExtendedAppearance.OffHandTint != appearance.ExtendedAppearance.OffHandTint)
                {
                    var weaponCharaBase = character.GetWeaponCharacterBase(ActorEquipSlot.OffHand);
                    if(weaponCharaBase != null)
                        weaponCharaBase->Tint = appearance.ExtendedAppearance.OffHandTint;
                }

                // Transparency
                if(existingAppearance.ExtendedAppearance.Transparency != appearance.ExtendedAppearance.Transparency)
                    native->Alpha = appearance.ExtendedAppearance.Transparency;

            }
        }

        return redrawResult;
    }

    public ActorAppearance GetActorAppearance(ICharacter character) => ActorAppearance.FromCharacter(character);

    private byte EnforceKindRestrictionsDetour(nint a1, nint a2)
    {
        if(_configurationService.Configuration.Appearance.ApplyNPCHack == ApplyNPCHack.Always)
            return 0;

        if(_configurationService.Configuration.Appearance.ApplyNPCHack == ApplyNPCHack.InGPose && _gPoseService.IsGPosing)
            return 0;

        return _enforceKindRestrictionsHook.Original(a1, a2);
    }

    private nint UpdateWetnessDetour(nint a1)
    {
        if(_gPoseService.IsGPosing)
            return 0;

        return _updateWetnessHook.Original(a1);
    }

    private nint UpdateTintDetour(nint charaBase, nint tint)
    {
        if(_gPoseService.IsGPosing && CanTint)
            return 0;

        return _updateTintHook.Original(charaBase, tint);
    }

    public void Dispose()
    {
        _enforceKindRestrictionsHook.Dispose();
        _updateWetnessHook.Dispose();
        _updateTintHook.Dispose();
        _actorLookAtLoop.Dispose();
    }
}

public record class LookAtDataHolder
{
    public LookAtTargetMode LookatType;

    public LookAtTargetType lookAtTargetType;
    public LookMode LookAtMode;

    public LookAtSource Target;

    public void SetTarget(Vector3 vector, LookAtTargetType targetType)
    {
        if(targetType.HasFlag(LookAtTargetType.Eyes))
            Target.Eyes.LookAtTarget.Position = vector;
        if(targetType.HasFlag(LookAtTargetType.Body))
            Target.Head.LookAtTarget.Position = vector;
        if(targetType.HasFlag(LookAtTargetType.Head))
            Target.Body.LookAtTarget.Position = vector;
    }
}

public enum LookAtTargetMode
{
    None,
    Forward,
    Camera,
    Position
}

public class LookAtData
{
    public uint EntityIdSource;
    public LookAtSource LookAtSource;

    public LookMode LookMode;
    public LookAtTargetType TargetType;

    public uint TargetEntityId;
    public Vector3 TargetPosition;
}

[Flags]
public enum LookAtTargetType
{
    Body = 0,
    Head = 1,
    Eyes = 2,

    Face = Head | Eyes,
    All = Body | Head | Eyes
}

[StructLayout(LayoutKind.Sequential)]
public struct LookAtSource
{
    public LookAtType Body;
    public LookAtType Head;
    public LookAtType Eyes;
    public LookAtType Unknown;
}

[StructLayout(LayoutKind.Explicit)]
public struct LookAtType
{
    [FieldOffset(0x30)] public LookAtTarget LookAtTarget;
}

[StructLayout(LayoutKind.Explicit)]
public struct LookAtTarget
{
    [FieldOffset(0x08)] public uint LookMode;
    [FieldOffset(0x10)] public Vector3 Position;
}

public enum LookEditType
{
    Body = 0,
    Head = 1,
    Eyes = 2
}

public enum LookMode
{
    None = 0,
    Frozen = 1,
    Pivot = 2,
    Position = 3,
}
