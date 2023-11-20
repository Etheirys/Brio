using Dalamud.Utility.Signatures;
using static FFXIVClientStructs.FFXIV.Client.Game.Character.Character;

namespace Brio.Game.Actor.Interop;
public unsafe class AttachmentInterop
{
    public delegate void CreateAndSetupMountDelegate(MountContainer* instance, short mountId, uint buddyModelTop, uint buddyModelBody, uint buddyModelLegs, byte buddyStain, byte unk6, byte unk7);
    [Signature("E8 ?? ?? ?? ?? 8B 43 ?? 41 89 46", ScanType = ScanType.Text)]
    public CreateAndSetupMountDelegate CreateAndSetupMount = null!;


    public delegate void SetupOrnamentDelegate(OrnamentContainer* instance, short ornamentId, uint param);
    [Signature("E8 ?? ?? ?? ?? 48 8B 7B ?? 0F B7 97", ScanType = ScanType.Text)]
    public SetupOrnamentDelegate SetupOrnament = null!;

    public delegate void SetupCompanionDelegate(CompanionContainer* instance, short companionId, uint param);
    [Signature("E8 ?? ?? ?? ?? 84 C0 74 ?? 66 44 89 7F", ScanType = ScanType.Text)]
    public SetupCompanionDelegate SetupCompanion = null!;

    public AttachmentInterop()
    {
        Dalamud.GameInteropProvider.InitializeFromAttributes(this);
    }
}
