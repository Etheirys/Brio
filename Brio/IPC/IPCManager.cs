
namespace Brio.IPC;

public class IPCManager
{
    public readonly PenumbraService Penumbra;
    public readonly GlamourerService Glamourer;
    public readonly CustomizePlusService CustomizePlus;
    public readonly DynamisService Dynamis;
    public readonly KtisisService Ktisis;

    public IPCManager(PenumbraService penumbraService, GlamourerService glamourerService, CustomizePlusService customizePlusService, DynamisService dynamisService, KtisisService ktisisService)
    {
        Penumbra = penumbraService;
        Glamourer = glamourerService;
        CustomizePlus = customizePlusService;
        Dynamis = dynamisService;
        Ktisis = ktisisService;
    }
}
