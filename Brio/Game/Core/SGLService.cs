using Brio.Services;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine.Group;

using CSLayoutTransform = FFXIVClientStructs.FFXIV.Client.LayoutEngine.Transform;

namespace Brio.Game.Core;

// SGL = SharedGroupLayout
public unsafe class SGLService : MediatorSubscriberBase
{
    public readonly IFramework _framework;

    private readonly delegate* unmanaged<short, uint, CSLayoutTransform*, byte*, byte*, byte, uint, int, nint, nint, SharedGroupLayoutInstance*> _createSGL;
    private readonly delegate* unmanaged<SharedGroupLayoutInstance**, nint, void> _destroySGL;
    private readonly delegate* unmanaged<SharedGroupLayoutInstance*, byte, void> _setupStainSGL;

    public SGLService(ISigScanner scanner, IFramework framework, Mediator mediator) : base(mediator)
    {
        _framework = framework;

        var createAddress = scanner.ScanText("E8 ?? ?? ?? ?? 48 89 04 ?? C6 44");
        _createSGL = (delegate* unmanaged<short, uint, CSLayoutTransform*, byte*, byte*, byte, uint, int, nint, nint, SharedGroupLayoutInstance*>)createAddress;

        var destroyAddress = scanner.ScanText("48 89 5C 24 08 48 89 74 24 10 57 48 83 EC 20 48 8B 19 48 8B F9 48 8B F2"); // TODO (KEN) clean up this sig and add wildcards 
        _destroySGL = (delegate* unmanaged<SharedGroupLayoutInstance**, nint, void>)destroyAddress;

        var setupStainAddress = scanner.ScanText("E8 ?? ?? ?? ?? 48 8B 8F ?? ?? ?? ?? 0F B6 47");
        _setupStainSGL = (delegate* unmanaged<SharedGroupLayoutInstance*, byte, void>)setupStainAddress;
    }

    public SharedGroupLayoutInstance* CreateSGL(string sgbPath, CSLayoutTransform transform)
    {
        var sgbBytes = System.Text.Encoding.UTF8.GetBytes(sgbPath + "\0");
        fixed (byte* sgbPathPtr = sgbBytes)
        {
            // magic numbers! the parameters are as follows:

            // -1 is the global layer while making sure we get attached with arg 8 being 0xC lets us own this object
            // 0, looks like a key for later lookup, if we pass 0 if genrates one for uss
            // the init transform
            // the path to the sgb file
            // - secondary path? I don't know what this is for, didn't look what it's used for
            // 1 looks like it's likely flags
            // 0, don't really know, it looks like it's fine to be 0
            // 0xC, this is an enum, a type of 'word object', I think this is used a quite a few places, but 0xC is for sgb files and lets us attach
            // 0 something to do with the parent?
            // 0 this looks like it's for some kind of bone attach? 

            Brio.Log.Warning("Creating SGL with path: " + sgbPath);

            return _createSGL(-1, 0, &transform, sgbPathPtr, null, 1, 0, 0xC, 0, 0);
        }
    }

    public void DestroySGL(SharedGroupLayoutInstance* instance)
    {
        _destroySGL(&instance, 0);
    }

    public void SetupStains(SharedGroupLayoutInstance* instance, byte defaultStain = 0)
    {
        _setupStainSGL(instance, defaultStain);
    }
}
