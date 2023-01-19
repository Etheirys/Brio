using Dalamud.Utility.Signatures;
using System;

namespace Brio.Game.Interop;

public class ClientObjectManagerInterop
{
    // TODO: All this should go back to FFXIV Client Structs

    [Signature("48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? C7 43 60 FF FF FF FF", ScanType = ScanType.StaticAddress)]
    private IntPtr Instance = IntPtr.Zero;

    private delegate uint CreateBattleCharacterDelegate(IntPtr instance, uint index, byte param);
    [Signature("E8 ?? ?? ?? ?? 83 F8 FF 74 C3", ScanType = ScanType.Text)]
    private CreateBattleCharacterDelegate _createBattleCharacter = null!;
    public uint CreateBattleCharacter(uint index = 0xFFFFFFFF, byte param = 0) => _createBattleCharacter(Instance, index, param);

    private delegate IntPtr GetObjectByIndexDelegate(IntPtr instance, ushort id);
    [Signature("E8 ?? ?? ?? ?? 48 8B F0 48 85 C0 74 4E 48 8B 10", ScanType = ScanType.Text)]
    private GetObjectByIndexDelegate _getObjectByIndex = null!;
    public IntPtr GetObjectByIndex(ushort id) => _getObjectByIndex(Instance, id);

    private delegate uint GetIndexByObjectDelegate(IntPtr instance, IntPtr character);
    [Signature("E8 ?? ?? ?? ?? 48 8B 5E ?? 8B E8", ScanType = ScanType.Text)]
    private GetIndexByObjectDelegate _getIndexByObject = null!;
    public uint GetIndexByObject(IntPtr character) => _getIndexByObject(Instance, character);

    private delegate void DeleteObjectByIndexDelegate(IntPtr instance, ushort id, byte param);
    [Signature("E8 ?? ?? ?? ?? C7 07 ?? ?? ?? ?? 48 8B 05", ScanType = ScanType.Text)]
    private DeleteObjectByIndexDelegate _deleteObjectByIndex = null!;
    public void DeleteObjectByIndex(ushort id, byte param) => _deleteObjectByIndex(Instance, id, param);

    private delegate uint CalculateNextIndexDelegate(IntPtr instance);
    [Signature("E8 ?? ?? ?? ?? 8B F8 83 F8 FF 75 12", ScanType = ScanType.Text)]
    private CalculateNextIndexDelegate _calculateNextIndex = null!;
    public uint CalculateNextIndex() => _calculateNextIndex(Instance);

    public ClientObjectManagerInterop()
    {
        SignatureHelper.Initialise(this);
    }

}
