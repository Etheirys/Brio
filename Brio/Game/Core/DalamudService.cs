using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Brio.Game.Core;

public class DalamudService : IDisposable
{
    public bool IsWine { get; init; }

    private readonly ICondition _condition;
    private readonly IFramework _framework;
    private readonly IClientState _clientState;
    private readonly IGameConfig _gameConfig;
    private readonly IDataManager _dataManager;
    private readonly IObjectTable _objectTable;

    private uint? _classJobId = 0;

    public DalamudService(ICondition condition, IObjectTable gameObjects, IClientState clientState, IGameConfig gameConfig, IDataManager dataManager, IFramework framework)
    {
        _condition = condition;
        _clientState = clientState;
        _framework = framework;
        _gameConfig = gameConfig;
        _dataManager = dataManager;
        _objectTable = gameObjects;

        IsWine = Util.IsWine();

        _framework.Update += FrameworkOnUpdate;
    }

    public bool IsInCutscene { get; private set; } = false;
    public bool IsZoning => _condition[ConditionFlag.BetweenAreas] || _condition[ConditionFlag.BetweenAreas51];

    public bool HasModifiedGameFiles => _dataManager.HasModifiedGameDataFiles;
    public bool IsLodEnabled { get; private set; }
    public uint ClassJobId => _classJobId!.Value;

    private void FrameworkOnUpdate(IFramework framework)
    {
        if(_condition[ConditionFlag.WatchingCutscene] && !IsInCutscene)
        {
            IsInCutscene = true;
        }
        else if(!_condition[ConditionFlag.WatchingCutscene] && IsInCutscene)
        {
            IsInCutscene = false;
        }

        var localPlayer = _clientState.LocalPlayer;
        if(localPlayer != null)
        {
            _classJobId = localPlayer.ClassJob.RowId;
        }

        if(_gameConfig != null
            && _gameConfig.TryGet(Dalamud.Game.Config.SystemConfigOption.LodType_DX11, out bool lodEnabled))
        {
            IsLodEnabled = lodEnabled;
        }
    }

    public async Task<uint> GetHomeWorldIdAsync()
    {
        return await RunOnFrameworkThread(GetHomeWorldId).ConfigureAwait(false);
    }

    public uint GetHomeWorldId()
    {
        return _clientState.LocalPlayer?.HomeWorld.RowId ?? 0;
    }

    public bool GetIsPlayerPresent()
    {
        return _clientState.LocalPlayer != null && _clientState.LocalPlayer.IsValid();
    }

    public async Task<bool> GetIsPlayerPresentAsync()
    {
        return await RunOnFrameworkThread(GetIsPlayerPresent).ConfigureAwait(false);
    }

    public string GetPlayerName()
    {
        return _clientState.LocalPlayer?.Name.ToString() ?? "--";
    }

    public bool IsObjectPresent(IGameObject? obj)
    {
        return obj != null && obj.IsValid();
    }

    public async Task<bool> IsObjectPresentAsync(IGameObject? obj)
    {
        return await RunOnFrameworkThread(() => IsObjectPresent(obj)).ConfigureAwait(false);
    }

    public async Task<string> GetPlayerNameAsync()
    {
        return await RunOnFrameworkThread(GetPlayerName).ConfigureAwait(false);
    }

    public async Task<IPlayerCharacter> GetPlayerCharacterAsync()
    {
        return await RunOnFrameworkThread(GetPlayerCharacter).ConfigureAwait(false);
    }
    public IPlayerCharacter GetPlayerCharacter()
    {
        return _clientState.LocalPlayer!;
    }

    public ICharacter? GetGposeCharacterFromObjectTableByName(string name, bool onlyGposeCharacters = false)
    {
        return (ICharacter?)_objectTable
             .FirstOrDefault(i => (!onlyGposeCharacters || i.ObjectIndex >= 200) && string.Equals(i.Name.ToString(), name, StringComparison.Ordinal));
    }

    public async Task<T> RunOnFrameworkThread<T>(Func<T> func, [CallerMemberName] string callerMember = "", [CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = 0)
    {
        if(!_framework.IsInFrameworkUpdateThread)
        {
            var result = await _framework.RunOnFrameworkThread(func).ContinueWith((task) => task.Result).ConfigureAwait(false);
            while(_framework.IsInFrameworkUpdateThread) // yield the thread again, should technically never be triggered
            {
                await Task.Delay(1).ConfigureAwait(false);
            }
            return result;
        }

        return func.Invoke();
    }

    public void Dispose()
    {
        _framework.Update -= FrameworkOnUpdate;

        GC.SuppressFinalize(this);
    }
}
