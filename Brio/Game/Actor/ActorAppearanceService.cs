using Brio.Config;
using Brio.Entities;
using Brio.Game.Actor.Appearance;
using Brio.Game.Actor.Extensions;
using Brio.Game.Camera;
using Brio.Game.Core;
using Brio.Game.GPose;
using Brio.IPC;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using System;
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
    private readonly PenumbraService _penumbraService;
    private readonly CustomizePlusService _customizePlusService;
    private readonly ActorRedrawService _actorRedrawService;
    private readonly CharacterHandlerService _characterHandlerService;
    private readonly DalamudService _dalamudService;

    private delegate byte EnforceKindRestrictionsDelegate(nint a1, nint a2);
    private readonly Hook<EnforceKindRestrictionsDelegate> _enforceKindRestrictionsHook = null!;

    private delegate nint UpdateWetnessDelegate(nint a1);
    private readonly Hook<UpdateWetnessDelegate> _updateWetnessHook = null!;

    private unsafe delegate nint UpdateTintDelegate(nint charaBase, nint tint);
    private readonly Hook<UpdateTintDelegate> _updateTintHook = null!;

    private unsafe delegate* unmanaged<DrawDataContainer*, ushort, ushort, void> _setFacewear;

    public bool CanTint => _configurationService.Configuration.Appearance.EnableTinting;

    public unsafe ActorAppearanceService(GPoseService gPoseService, VirtualCameraManager virtualCameraManager, CharacterHandlerService characterHandlerService,
        IObjectTable objectTable, CustomizePlusService customizePlusService, PenumbraService penumbraService, ActorRedrawService actorRedrawService, DalamudService dalamudService,
        ConfigurationService configurationService, ActorRedrawService redrawService, GlamourerService glamourerService, EntityManager entityManager,
        ISigScanner sigScanner, IGameInteropProvider hooks)
    {
        _gPoseService = gPoseService;
        _configurationService = configurationService;
        _redrawService = redrawService;
        _glamourerService = glamourerService;
        _customizePlusService = customizePlusService;
        _entityManager = entityManager;
        _objectTable = objectTable;
        _virtualCameraManager = virtualCameraManager;
        _penumbraService = penumbraService;
        _actorRedrawService = actorRedrawService;
        _dalamudService = dalamudService;
        _characterHandlerService = characterHandlerService;

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
        _setFacewear = (delegate* unmanaged<DrawDataContainer*, ushort, ushort, void>)setFacewearAddress;
    }

    //

    public async Task<RedrawResult> Redraw(ICharacter character, bool revert)
    {
        if(revert)
            await _characterHandlerService.Revert(character);

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
                        appearance.Facewear = native->DrawData.GlassesIds[0];
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
            _glamourerService.UnlockAndRevertCharacter(character);

            needsRedraw = true;
        }

        RedrawResult redrawResult = RedrawResult.Optmized;

        if(needsRedraw)
            redrawResult = await _redrawService.Redraw(character);

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

                // Viera Ears
                if(existingAppearance.Runtime.IsVieraEarsHidden != appearance.Runtime.IsVieraEarsHidden || forceHeadToggles)
                {
                    native->DrawData.VieraEarsHidden = appearance.Runtime.IsVieraEarsHidden;
                    native->DrawData.HideVieraEars(appearance.Runtime.IsVieraEarsHidden);
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

        GC.SuppressFinalize(this);
    }
}
