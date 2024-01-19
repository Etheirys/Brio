using Brio.Game.Actor.Extensions;
using Brio.Game.Actor.Interop;
using Dalamud.Game;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using NativeCharacter = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;

namespace Brio.Game.Core;

internal unsafe class ObjectMonitorService : IDisposable
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

    private readonly Dictionary<nint, Character> _charaBaseToCharacterCache = [];

    public ObjectMonitorService(IObjectTable objectTable, ISigScanner scanner, IGameInteropProvider hooking)
    {
        _objectTable = objectTable;

        var charInitializeAddress = scanner.ScanText("E8 ?? ?? ?? ?? 8D 57 ?? C6 83");
        _characterIntitializeHook = hooking.HookFromAddress<NativeCharacterEventDelegate>(charInitializeAddress, CharacterIntitializeDetour);
        _characterIntitializeHook.Enable();

        var charFinalizeAddress = scanner.ScanText("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC ?? 48 8D 05 ?? ?? ?? ?? 48 8B D9 48 89 01 48 8D 05 ?? ?? ?? ?? 48 89 81 ?? ?? ?? ?? 48 81 C1");
        _characterFinalizeHook = hooking.HookFromAddress<NativeCharacterEventDelegate>(charFinalizeAddress, CharacterFinalizeDetour);
        _characterFinalizeHook.Enable();

        var charaBaseCleanupAddr = Marshal.ReadInt64((nint)(CharacterBase.StaticAddressPointers.VTable + 0x8));
        _characterBaseCleanupHook = hooking.HookFromAddress<CharacterBaseCleanupDelegate>((nint)charaBaseCleanupAddr, CharacterBaseCleanupDetour);
        _characterBaseCleanupHook.Enable();

        var charaBaseUpdateMaterialsDetour = "40 53 48 83 EC ?? 48 8B D9 E8 ?? ?? ?? ?? 48 8B 8B ?? ?? ?? ?? 48 85 C9 74 ?? 4C 8B 83";
        _characterBaseUpdateMaterialsHook = hooking.HookFromAddress<CharacterBaseUpdateMaterialsDelegate>(scanner.ScanText(charaBaseUpdateMaterialsDetour), CharacterBaseUpdateMaterialsDetour);
        _characterBaseUpdateMaterialsHook.Enable();
    }

    public bool TryGetCharacterFromCharacterBase(BrioCharacterBase* characterBase, [MaybeNullWhen(false)] out Character chara) => TryGetCharacterFromCharacterBase((CharacterBase*)characterBase, out chara);

    public bool TryGetCharacterFromCharacterBase(CharacterBase* characterBase, [MaybeNullWhen(false)] out Character chara)
    {
        if(_charaBaseToCharacterCache.TryGetValue((nint)characterBase, out chara))
            return true;

        foreach(var obj in _objectTable)
        {
            if(obj is Character foundChara)
            {
                var bases = foundChara.GetCharacterBases();
                foreach(var searchBase in bases)
                {
                    if(searchBase.CharacterBase == characterBase)
                    {
                        chara = foundChara;
                        _charaBaseToCharacterCache[(nint)characterBase] = foundChara;
                        return true;
                    }
                }
            }
        }
        chara = null;
        return false;
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
            _charaBaseToCharacterCache.Remove((nint)charaBase);
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
