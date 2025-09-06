using Brio.Game.Core;
using Brio.Game.GPose;
using Brio.IPC;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Brio.Game.Actor;

public record CharacterHolder(IGameObject GameObject, Guid? CPlusID, string Name);

public class CharacterHandlerService : IDisposable
{
    private readonly IFramework _framework;
    private readonly ActorRedrawService _redrawService;
    private readonly GPoseService _gPoseService;
    private readonly DalamudService _dalamudService;

    private readonly PenumbraService _penumbraService;
    private readonly GlamourerService _glamourerService;
    private readonly CustomizePlusService _customizePlusService;

    //

    public HashSet<CharacterHolder> CharacterHandler = [];

    public CharacterHandlerService(IFramework framework, DalamudService dalamudService, GPoseService gPoseService, ActorRedrawService redrawService,
        PenumbraService penumbraService, GlamourerService glamourerService, CustomizePlusService customizePlusService)
    {
        _framework = framework;
        _redrawService = redrawService;
        _gPoseService = gPoseService;
        _dalamudService = dalamudService;

        _penumbraService = penumbraService;
        _glamourerService = glamourerService;
        _customizePlusService = customizePlusService;

        _gPoseService.OnGPoseStateChange += OnGPoseStateChange;
    }

    private void OnGPoseStateChange(bool newState)
    {
        if(newState == false)
        {
            foreach(var entry in CharacterHandler)
            {
                var character = _dalamudService.GetGposeCharacterFromObjectTableByName(entry.Name, onlyGposeCharacters: true);
                if(character is null)
                {
                    CharacterHandler.Remove(entry);
                    RevertMCDF(entry).GetAwaiter().GetResult();
                }
            }
        }
    }

    public async Task RevertMCDF(CharacterHolder mCDFCharacterHolder)
    {
        if(mCDFCharacterHolder.GameObject.Address == nint.Zero || mCDFCharacterHolder.Name.IsNullOrEmpty()) return;
    
        _glamourerService.UnlockAndRevertCharacter(mCDFCharacterHolder.GameObject);
        _glamourerService.UnlockAndRevertCharacterByName(mCDFCharacterHolder.Name);

        if(mCDFCharacterHolder.GameObject.Address != nint.Zero)
        {
            _customizePlusService.RemoveTemporaryProfile(mCDFCharacterHolder.GameObject);
            await _penumbraService.Redraw(mCDFCharacterHolder.GameObject, true).ConfigureAwait(false);
        }
    }

    public async Task Revert(IGameObject obj, bool afterGpose = false)
    {
        if(obj.Address == nint.Zero) return;

        _glamourerService.UnlockAndRevertCharacterByName(obj.Name.TextValue);
        _glamourerService.UnlockAndRevertCharacter(obj);

        _customizePlusService.RemoveTemporaryProfile(obj);

        if(obj.Address != nint.Zero)
        {
            if(afterGpose == false)
                await _redrawService.RedrawAndWait(obj).ConfigureAwait(false);
            await _penumbraService.Redraw(obj, afterGpose).ConfigureAwait(false);
        }
    }

    public async Task RevertHandledChara(CharacterHolder? holder)
    {
        if(holder == null) return;
        CharacterHandler.Remove(holder);
        await _framework.RunOnTick(async () => await RevertMCDF(holder));
    }

    public void Dispose()
    {
        _gPoseService.OnGPoseStateChange -= OnGPoseStateChange;

        foreach(var character in CharacterHandler)
        {
            _ = RevertHandledChara(character);
        }

        CharacterHandler.Clear();

        GC.SuppressFinalize(this);
    }
}
