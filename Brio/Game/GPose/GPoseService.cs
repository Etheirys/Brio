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
            OnGPoseStateChange?.Invoke(_lastGPoseState);
        }
    }

    public List<GameObject> GPoseObjects
    {
        get
        {
            List<GameObject> objects = new();
            for(int i = 201; i < 240; ++i)
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
