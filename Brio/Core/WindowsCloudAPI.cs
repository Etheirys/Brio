
//
// This is from Penumbra; CloudApi.cs by Ny, Penumbra by Ottermandias [https://github.com/xivdev/Penumbra/blob/13500264b7f046cacbddae41df704089be7e7908/Penumbra/Interop/CloudApi.cs]
//

using System;
using System.Runtime.InteropServices;

namespace Brio.Core;

public static unsafe partial class WindowsCloudAPI
{
    private const int CfSyncRootInfoBasic = 0;

    /// <summary> Determines whether a file or directory is cloud-synced using OneDrive or other providers that use the Cloud API. </summary>
    /// <remarks> Can be expensive. Callers should cache the result when relevant. </remarks>
    public static bool IsCloudSynced(string path)
    {
        var buffer = stackalloc long[1];
        int hresult;
        uint length;

        try
        {
            hresult = CfGetSyncRootInfoByPath(path, CfSyncRootInfoBasic, buffer, sizeof(long), out length);
        }
        catch(DllNotFoundException)
        {
            Brio.Log.Debug($"{nameof(CfGetSyncRootInfoByPath)} threw DllNotFoundException");
            return false;
        }
        catch(EntryPointNotFoundException)
        {
            Brio.Log.Debug($"{nameof(CfGetSyncRootInfoByPath)} threw EntryPointNotFoundException");
            return false;
        }

        Brio.Log.Debug($"{nameof(CfGetSyncRootInfoByPath)} returned HRESULT 0x{hresult:X8}");
        if(hresult < 0)
        {
            return false;
        }

        if(length != sizeof(long))
        {
            Brio.Log.Debug($"Expected {nameof(CfGetSyncRootInfoByPath)} to return {sizeof(long)} bytes, got {length} bytes");
            return false;
        }

        Brio.Log.Debug($"{nameof(CfGetSyncRootInfoByPath)} returned {{ SyncRootFileId = 0x{*buffer:X16} }}");

        return true;
    }

    [LibraryImport("cldapi.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int CfGetSyncRootInfoByPath(string filePath, int infoClass, void* infoBuffer, uint infoBufferLength, out uint returnedLength);
}
