using Brio.Config;
using Brio.Entities;
using Brio.Game.Actor.Appearance;
using Brio.Game.Actor.Extensions;
using Brio.Game.GPose;
using Brio.IPC;
using Dalamud.Game;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static Brio.Game.Actor.ActorRedrawService;
using DrawDataContainer = FFXIVClientStructs.FFXIV.Client.Game.Character.DrawDataContainer;

namespace Brio.Game.Actor;

internal class ActorAppearanceService : IDisposable
{
    private readonly GPoseService _gPoseService;
    private readonly ConfigurationService _configurationService;
    private readonly ActorRedrawService _redrawService;
    private readonly GlamourerService _glamourerService;
    private readonly EntityManager _entityManager;

    private delegate byte EnforceKindRestrictionsDelegate(nint a1, nint a2);
    private readonly Hook<EnforceKindRestrictionsDelegate> _enforceKindRestrictionsHook = null!;

    private delegate nint UpdateWetnessDelegate(nint a1);
    private readonly Hook<UpdateWetnessDelegate> _updateWetnessHook = null!;

    private unsafe delegate nint UpdateTintDelegate(nint charaBase, nint tint);
    private readonly Hook<UpdateTintDelegate> _updateTintHook = null!;

    private uint _forceNpcHackCount = 0;

    public bool CanTint => _configurationService.Configuration.Appearance.EnableTinting;

    public unsafe ActorAppearanceService(GPoseService gPoseService, ConfigurationService configurationService, ActorRedrawService redrawService, GlamourerService glamourerService, EntityManager entityManager, ISigScanner sigScanner, IGameInteropProvider hooks)
    {
        _gPoseService = gPoseService;
        _configurationService = configurationService;
        _redrawService = redrawService;
        _glamourerService = glamourerService;
        _entityManager = entityManager;

        var enforceKindRestrictionsAddress = sigScanner.ScanText("E8 ?? ?? ?? ?? 41 B0 ?? 48 8B D3 48 8B CD");
        _enforceKindRestrictionsHook = hooks.HookFromAddress<EnforceKindRestrictionsDelegate>(enforceKindRestrictionsAddress, EnforceKindRestrictionsDetour);
        _enforceKindRestrictionsHook.Enable();

        var updateWetnessAddress = sigScanner.ScanText("E8 ?? ?? ?? ?? 8B 83 ?? ?? ?? ?? C1 E8 ?? A8 ?? 74 ?? 48 8B 8B ?? ?? ?? ?? 48 85 C9");
        _updateWetnessHook = hooks.HookFromAddress<UpdateWetnessDelegate>(updateWetnessAddress, UpdateWetnessDetour);
        _updateWetnessHook.Enable();

        var updateTintHook = Marshal.ReadInt64((nint)(CharacterBase.StaticAddressPointers.VTable + 0xC0));
        _updateTintHook = hooks.HookFromAddress<UpdateTintDelegate>((nint)updateTintHook, UpdateTintDetour);
        _updateTintHook.Enable();
    }

    public void PushForceNpcHack() => ++_forceNpcHackCount;
    public void PopForceNpcHack()
    {
        if(_forceNpcHackCount == 0)
            throw new Exception("Invalid _forceNpcHack count (is already 0)");

        --_forceNpcHackCount;
    }

    public IDisposable ForceNpcHack()
    {
        PushForceNpcHack();
        return new HackDisposer(this);
    }

    public async Task<RedrawResult> Redraw(Character character)
    {
        var appearance = GetActorAppearance(character);
        return await SetCharacterAppearance(character, appearance, AppearanceImportOptions.All, true);
    }

    public async Task<RedrawResult> SetCharacterAppearance(Character character, ActorAppearance appearance, AppearanceImportOptions options, bool forceRedraw = false)
    {
        var existingAppearance = GetActorAppearance(character);

        AppearanceSanitizer.SanitizeAppearance(ref appearance, existingAppearance);

        bool needsRedraw = forceRedraw;
        bool forceHeadToggles = false;
        bool glamourerReset = false;

        unsafe
        {
            var native = character.Native();

            // Model
            if(native->CharacterData.ModelCharaId != appearance.ModelCharaId)
            {
                native->CharacterData.ModelCharaId = appearance.ModelCharaId;
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

                            byte[] data = new byte[68];
                            fixed(byte* ptr = data)
                            {
                                if(options.HasFlag(AppearanceImportOptions.Customize))
                                {
                                    Buffer.MemoryCopy(appearance.Customize.Data, ptr, 28, 28);
                                }
                                else
                                {
                                    Buffer.MemoryCopy(existingAppearance.Customize.Data, ptr, 28, 28);
                                }

                                if(options.HasFlag(AppearanceImportOptions.Equipment))
                                {
                                    Buffer.MemoryCopy(appearance.Equipment.Data, ptr + 28, 40, 40);
                                }
                                else
                                {
                                    Buffer.MemoryCopy(existingAppearance.Equipment.Data, ptr + 28, 40, 40);
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
                        *(ActorEquipment*)&native->DrawData.Head = appearance.Equipment;
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
            }


            if(options.HasFlag(AppearanceImportOptions.Weapon))
            {
                // Weapon Visibility
                if(existingAppearance.Runtime.IsMainHandHidden != appearance.Runtime.IsMainHandHidden)
                    character.GetWeaponDrawObjectData(ActorEquipSlot.MainHand)->IsHidden = appearance.Runtime.IsMainHandHidden;


                if(existingAppearance.Runtime.IsOffHandHidden != appearance.Runtime.IsOffHandHidden)
                    character.GetWeaponDrawObjectData(ActorEquipSlot.OffHand)->IsHidden = appearance.Runtime.IsOffHandHidden;

            }
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
                    charaBase->CharacterBase.SwimmingWetness = appearance.ExtendedAppearance.Wetness;
                }
                if(existingAppearance.ExtendedAppearance.WetnessDepth != appearance.ExtendedAppearance.WetnessDepth)
                {
                    var charaBase = character.GetCharacterBase();
                    charaBase->CharacterBase.WetnessDepth = appearance.ExtendedAppearance.WetnessDepth;
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

    public ActorAppearance GetActorAppearance(Character character) => ActorAppearance.FromCharacter(character);

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
    }

    private class HackDisposer(ActorAppearanceService service) : IDisposable
    {
        public void Dispose()
        {
            service.PopForceNpcHack();
        }
    }
}
