using Brio.API;
using Brio.API.Interface;
using Brio.Config;
using System;

namespace Brio.IPC.API;

public class BrioAPIService(ConfigurationService configurationService, StateAPI stateAPI, ActorAPI actorAPI, EnvironmentAPI environmentAPI, PosingAPI posingAPI, AnimationAPI animationAPI) : IBrioAPI, IDisposable
{
    public const int MajorVersion = 3;
    public const int MinorVersion = 0;

    private readonly ConfigurationService _configurationService = configurationService;
  
    public bool IsIPCEnabled => _configurationService.Configuration.IPC.EnableBrioIPC;


    public bool Valid { get; private set; } = true;


    public IState State { get; } = stateAPI;

    public IActor Actor { get; } = actorAPI;

    public IEnvironment Environment { get; } = environmentAPI;

    public IPosing Posing { get; } = posingAPI;

    public IAnimation Animation { get; } = animationAPI;

    public void Dispose()
    {
        Valid = false;
    }
}
