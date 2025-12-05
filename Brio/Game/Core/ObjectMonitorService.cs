using Brio.Game.Actor.Interop;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using System;
using System.Runtime.InteropServices;
using NativeCharacter = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;

namespace Brio.Game.Core;

public unsafe class ObjectMonitorService : IDisposable
{
    public IObjectTable ObjectTable => _objectTable;

    public delegate void CharacterEventDelegate(NativeCharacter* chara);
    public event CharacterEventDelegate? CharacterInitialized;
    public event CharacterEventDelegate? CharacterDestroyed;

    public delegate void CharacterBaseEventDelegate(BrioCharacterBase* charaBase);
    public event CharacterBaseEventDelegate? CharacterBaseMaterialsUpdated;
    public event CharacterBaseEventDelegate? CharacterBaseDestroyed;

    private readonly IObjectTable _objectTable;

    private delegate nint NativeCharacterEventDelegate(NativeCharacter* chara);
    private readonly Hook<NativeCharacterEventDelegate> _characterIntitializeHook = null!;
    private readonly Hook<NativeCharacterEventDelegate> _characterFinalizeHook = null!;

    private delegate nint CharacterBaseUpdateMaterialsDelegate(BrioCharacterBase* charaBase);
    private readonly Hook<CharacterBaseUpdateMaterialsDelegate> _characterBaseUpdateMaterialsHook = null!;

    private delegate nint CharacterBaseCleanupDelegate(BrioCharacterBase* charaBase);
    private readonly Hook<CharacterBaseCleanupDelegate> _characterBaseCleanupHook = null!;

    public ObjectMonitorService(IObjectTable objectTable, ISigScanner scanner, IGameInteropProvider hooking)
    {
        _objectTable = objectTable;

        var charInitializeAddress = scanner.ScanText("E8 ?? ?? ?? ?? 8D 57 ?? C6 83");
        _characterIntitializeHook = hooking.HookFromAddress<NativeCharacterEventDelegate>(charInitializeAddress, CharacterIntitializeDetour);
        _characterIntitializeHook.Enable();

        var charFinalizeAddress = scanner.ScanText("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC ?? 48 8D 05 ?? ?? ?? ?? 48 8B D9 48 89 01 48 8D 05 ?? ?? ?? ?? 48 89 81 ?? ?? ?? ?? 48 81 C1");
        _characterFinalizeHook = hooking.HookFromAddress<NativeCharacterEventDelegate>(charFinalizeAddress, CharacterFinalizeDetour);
        _characterFinalizeHook.Enable();

        var charaBaseCleanupAddr = Marshal.ReadInt64((nint)(CharacterBase.StaticVirtualTablePointer) + 8);
        _characterBaseCleanupHook = hooking.HookFromAddress<CharacterBaseCleanupDelegate>((nint)charaBaseCleanupAddr, CharacterBaseCleanupDetour);
        _characterBaseCleanupHook.Enable();

        var charaBaseUpdateMaterialsDetour = "48 89 5C 24 ?? 48 89 6C 24 ?? 56 57 41 56 48 83 EC ?? 4C 89 7C 24";
        _characterBaseUpdateMaterialsHook = hooking.HookFromAddress<CharacterBaseUpdateMaterialsDelegate>(scanner.ScanText(charaBaseUpdateMaterialsDetour), CharacterBaseUpdateMaterialsDetour);
        _characterBaseUpdateMaterialsHook.Enable();
    }

    private nint CharacterIntitializeDetour(NativeCharacter* chara)
    {
        var result = _characterIntitializeHook.Original.Invoke(chara);
        CharacterInitialized?.Invoke(chara);
        return result;
    }

    private nint CharacterFinalizeDetour(NativeCharacter* chara)
    {
        CharacterDestroyed?.Invoke(chara);
        return _characterFinalizeHook.Original.Invoke(chara);
    }

    private nint CharacterBaseUpdateMaterialsDetour(BrioCharacterBase* charaBase)
    {
        var result = _characterBaseUpdateMaterialsHook.Original(charaBase);
        CharacterBaseMaterialsUpdated?.Invoke(charaBase);
        return result;
    }

    private nint CharacterBaseCleanupDetour(BrioCharacterBase* charaBase)
    {
        if(charaBase != null)
        {
            CharacterBaseDestroyed?.Invoke(charaBase);
        }

        return _characterBaseCleanupHook.Original(charaBase);
    }

    public void Dispose()
    {
        _characterIntitializeHook.Dispose();
        _characterFinalizeHook.Dispose();
        _characterBaseUpdateMaterialsHook.Dispose();
        _characterBaseCleanupHook.Dispose();
    }
}
