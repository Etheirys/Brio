using Dalamud.Game.ClientState.Objects.Types;
using System;
using System.Collections.Generic;

namespace Brio.Game.GPose;

public class GPoseService : IDisposable
{
    public bool IsInGPose => Dalamud.PluginInterface.UiBuilder.GposeActive;

    private bool _lastGPoseState;

    public delegate void OnGPoseStateChangeDelegate(bool isInGpose);
    public event OnGPoseStateChangeDelegate? OnGPoseStateChange;

    private const int GPoseFirstActor = 201;
    private const int GPoseActorCount = 39;

    public GPoseService()
    {
        Dalamud.Framework.Update += Framework_Update;

        _lastGPoseState = IsInGPose;
    }

    private void Framework_Update(global::Dalamud.Game.Framework framework)
    {
        if(_lastGPoseState != IsInGPose)
        {
            _lastGPoseState = IsInGPose;
            HandleGPoseChange(_lastGPoseState);
        }
    }

    private void HandleGPoseChange(bool newGPoseState)
    {
        if(newGPoseState)
        {
            if (Brio.Configuration.OpenBrioBehavior == Config.OpenBrioBehavior.OnGPoseEnter)
                Brio.UI.MainWindow.IsOpen = true;
        }

        OnGPoseStateChange?.Invoke(newGPoseState);
    }

    public List<GameObject> GPoseObjects
    {
        get
        {
            List<GameObject> objects = new();
            for(int i = GPoseFirstActor; i < GPoseFirstActor + GPoseActorCount; ++i)
            {
                var go = Dalamud.ObjectTable[i];
                if (go != null)
                    objects.Add(go);
            }

            return objects;
        }
    }

    public void Dispose()
    {
        Dalamud.Framework.Update -= Framework_Update;
    }
}
