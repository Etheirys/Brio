
// From Tuples.cs from "CustomizePlus" https://github.com/Aether-Tools/CustomizePlus

global using IPCProfileDataTuple = (
    System.Guid UniqueId,
    string Name,
    string VirtualPath,
    System.Collections.Generic.List<(string Name, ushort WorldId, byte CharacterType, ushort CharacterSubType)> Characters,
    int Priority,
    bool IsEnabled);

// ---------------------------------------------------------------------------------

using Brio.Config;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Brio.IPC;

public class CustomizePlusService : BrioIPC
{
    public override string Name { get; } = "Customize+";

    public override bool IsAvailable
        => CheckStatus() == IPCStatus.Available;
    public override bool AllowIntegration
        => _configurationService.Configuration.IPC.AllowCustomizePlusIntegration;

    public override int APIMajor => 6;
    public override int APIMinor => 0;

    public override (int Major, int Minor) GetAPIVersion()
        => _customizeplusApiVersion.InvokeFunc();

    public override IDalamudPluginInterface GetPluginInterface()
        => _pluginInterface;
    //

    private readonly ConfigurationService _configurationService;
    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly ICommandManager _commandManager;
    private readonly IObjectTable _gameObjects;
    private readonly IFramework _framework;

    private readonly ICallGateSubscriber<ushort, string, (int, Guid?)> _customizeplusSetTemporaryProfile;
    private readonly ICallGateSubscriber<IList<IPCProfileDataTuple>> _customizeplusGetAllProfiles;
    private readonly ICallGateSubscriber<ushort, (int, Guid?)> _customizeplusGetActiveProfileId;
    private readonly ICallGateSubscriber<Guid, (int, string?)> _customizeplusGetProfileById;
    private readonly ICallGateSubscriber<ushort, int> _customizeplusDeleteTemporaryProfile;
    private readonly ICallGateSubscriber<Guid, int> _customizePlusDeleteByUniqueId;
    private readonly ICallGateSubscriber<(int, int)> _customizeplusApiVersion;
    private readonly ICallGateSubscriber<bool> _customizeplusIsValid;


    public CustomizePlusService(IDalamudPluginInterface pluginInterface, ICommandManager commandManager, IObjectTable gameObjects, ConfigurationService configurationService, IFramework framework)
    {
        _pluginInterface = pluginInterface;
        _configurationService = configurationService;
        _framework = framework;
        _gameObjects = gameObjects;
        _commandManager = commandManager;

        _customizeplusApiVersion = _pluginInterface.GetIpcSubscriber<(int, int)>("CustomizePlus.General.GetApiVersion");
        _customizeplusIsValid = _pluginInterface.GetIpcSubscriber<bool>("CustomizePlus.General.IsValid");

        _customizeplusSetTemporaryProfile = _pluginInterface.GetIpcSubscriber<ushort, string, (int, Guid?)>("CustomizePlus.Profile.SetTemporaryProfileOnCharacter");
        _customizeplusDeleteTemporaryProfile = _pluginInterface.GetIpcSubscriber<ushort, int>("CustomizePlus.Profile.DeleteTemporaryProfileOnCharacter");

        _customizeplusGetAllProfiles = _pluginInterface.GetIpcSubscriber<IList<IPCProfileDataTuple>>("CustomizePlus.Profile.GetList");
        _customizeplusGetActiveProfileId = _pluginInterface.GetIpcSubscriber<ushort, (int, Guid?)>("CustomizePlus.Profile.GetActiveProfileIdOnCharacter");
        _customizeplusGetProfileById = _pluginInterface.GetIpcSubscriber<Guid, (int, string?)>("CustomizePlus.Profile.GetByUniqueId");
       
        _customizePlusDeleteByUniqueId = _pluginInterface.GetIpcSubscriber<Guid, int>("CustomizePlus.Profile.DeleteTemporaryProfileByUniqueId");

        _configurationService.OnConfigurationChanged += OnConfigurationChanged;
        OnConfigurationChanged();
    }

    public void OpenCustomizePlus()
    {
        _commandManager.ProcessCommand("/c+");
    }

    public bool RemoveTemporaryProfile(IGameObject? character)
    {
        if(IsAvailable == false && character is not null)
            return false;

        _customizeplusDeleteTemporaryProfile.InvokeFunc(character!.ObjectIndex);

        return true;
    }

    public async Task RevertByIdAsync(Guid? profileId)
    {
        if(IsAvailable == false || profileId == null) return;

        await _framework.RunOnFrameworkThread(() =>
        {
            _ = _customizePlusDeleteByUniqueId.InvokeFunc(profileId.Value);
        }).ConfigureAwait(false);
    }

    public (string?, Guid?) GetActiveProfile(IGameObject? character)
    {
        if(IsAvailable == false && character is not null)
            return (string.Empty, null);

        var (_, id) = _customizeplusGetActiveProfileId.InvokeFunc(character!.ObjectIndex);

        var data = string.Empty;
        if(id != null)
            (_, data) = _customizeplusGetProfileById.InvokeFunc(id.Value);

        return (data, id);
    }

    public (int, string?) GetProfile(Guid id)
    {
        if(IsAvailable == false && id != default)
            return (255, string.Empty);

        return _customizeplusGetProfileById.InvokeFunc(id);
    }

    public bool SetProfile(IGameObject? character, string profileData)
    {
        if(IsAvailable == false && string.IsNullOrEmpty(profileData) && character is not null)
            return false;

        _customizeplusSetTemporaryProfile.InvokeFunc(character!.ObjectIndex, profileData);

        return true;
    }

    public IEnumerable<IPCProfileDataTuple> GetProfiles()
    {
        if(IsAvailable == false)
            return [];

        return _customizeplusGetAllProfiles.InvokeFunc();
    }

    public async Task<Guid?> SetBodyScaleAsync(IGameObject gameObj, string scale)
    {
        if(IsAvailable == false) return null;

        return await _framework.RunOnFrameworkThread(() =>
        {
            if(gameObj is ICharacter c)
            {
                string decodedScale = Encoding.UTF8.GetString(Convert.FromBase64String(scale));
                if(scale.IsNullOrEmpty())
                {
                    _customizeplusDeleteTemporaryProfile!.InvokeFunc(c.ObjectIndex);
                    return null;
                }
                else
                {
                    var result = _customizeplusSetTemporaryProfile!.InvokeFunc(c.ObjectIndex, decodedScale);
                    return result.Item2;
                }
            }

            return null;
        }).ConfigureAwait(false);
    }
    
    public async Task<string?> GetScaleAsync(nint character)
    {
        if(IsAvailable == false) return null;

        var scale = await _framework.RunOnFrameworkThread(() =>
        {
            var gameObj = _gameObjects.CreateObjectReference(character);
            if(gameObj is ICharacter c)
            {
                var res = _customizeplusGetActiveProfileId.InvokeFunc(c.ObjectIndex);
                Brio.Log.Debug("CustomizePlus GetActiveProfile returned {err}", res.Item1);
                if(res.Item1 != 0 || res.Item2 == null) return string.Empty;
                return _customizeplusGetProfileById.InvokeFunc(res.Item2.Value).Item2;
            }

            return string.Empty;
        }).ConfigureAwait(false);
        if(string.IsNullOrEmpty(scale)) return string.Empty;
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(scale));
    }

    private void OnConfigurationChanged()
        => CheckStatus();

    public override void Dispose()
    {
        _configurationService.OnConfigurationChanged -= OnConfigurationChanged;
    }
}
