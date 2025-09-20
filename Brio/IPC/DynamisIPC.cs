
//
// The power of Dynamis Compels You !!!! (Thank you Ny, You have made my life so much better for making Dynamis)
// https://github.com/Exter-N/Dynamis/
//

//
// This files is licensed to you under the Apache License, Version 2.0.
// The full license can be viewed at the bottom of this file 
//
// Most of the code found in this file is based on DynamisIpc.cs from OtterGui
// https://github.com/Ottermandias/OtterGui/blob/f354444776591ae423e2d8374aae346308d81424/Services/DynamisIpc.cs
//
// List of major changes made:
// - Integrated code so it can be used for BrioIPC
// - Changed logging to use Brio.Log
// - Added static `Instance` property for easy access
//

using Brio.Config;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using System;
using System.Numerics;

namespace Brio.IPC;

public class DynamisIPC : BrioIPC
{
    public static DynamisIPC? Instance { get; private set; }

    public override string Name { get; } = "Dynamis";

    public override bool IsAvailable
        => CheckStatus() == IPCStatus.Available;

    public override bool AllowIntegration
        => _configurationService.IsDebug;

    public override int APIMajor => 1;
    public override int APIMinor => 6;

    public override (int Major, int Minor) GetAPIVersion() =>
        _getApiVersion.InvokeFunc() is { Major: var major, Minor: var minor }
            ? ((int)major, (int)minor)
            : (0, 0);

    public override IDalamudPluginInterface GetPluginInterface()
        => _pluginInterface;

    //

    private readonly ConfigurationService _configurationService;
    private readonly IDalamudPluginInterface _pluginInterface;

    //
    // 
    //

    private readonly ICallGateSubscriber<(uint Major, uint Minor, ulong Flags)> _getApiVersion;
    private readonly ICallGateSubscriber<uint, uint, ulong, Version, object?> _initialized;
    private readonly ICallGateSubscriber<object?> _disposed;

    //

    private Action<nint, Func<string?>?, string?, ulong, Vector2>? _drawPointerAction;
    private ICallGateSubscriber<nint, object?>? _imGuiDrawPointerTooltipDetails;
    private ICallGateSubscriber<nint, Func<string?>?, object?>? _imGuiOpenPointerContextMenu;

    private ICallGateSubscriber<nint, string?, object?>? _inspectObject;
    private ICallGateSubscriber<nint, uint, string, uint, uint, string?, object?>? _inspectRegion;
    private ICallGateSubscriber<nint, (string, Type?, uint, uint)>? _getClass;
    private ICallGateSubscriber<nint, string?, Type?, (bool, uint)>? _isInstanceOf;
    private ICallGateSubscriber<object?>? _preloadDataYaml;

    //

    public bool IsAttached => VersionMajor > 0;

    public ulong Features { get; private set; }
    public uint VersionMajor { get; private set; }
    public uint VersionMinor { get; private set; }

    public Exception? Error { get; private set; }

    //

    public DynamisIPC(ConfigurationService configurationService, IDalamudPluginInterface pluginInterface)
    {
        _configurationService = configurationService;
        _pluginInterface = pluginInterface;

        _getApiVersion = _pluginInterface.GetIpcSubscriber<(uint Major, uint Minor, ulong Flags)>("Dynamis.GetApiVersion");

        _initialized = _pluginInterface.GetIpcSubscriber<uint, uint, ulong, Version, object?>("Dynamis.ApiInitialized");
        _initialized.Subscribe(OnInitialized);
        _disposed = _pluginInterface.GetIpcSubscriber<object?>("Dynamis.ApiDisposing");
        _disposed.Subscribe(OnDisposed);

        if(_getApiVersion.InvokeFunc() is { Major: var major, Minor: var minor, Flags: var flags })
            OnInitialized(major, minor, flags, null!);

        Instance = this;
    }

    //

    public void InspectObject(nint address, string? name = null)
        => _inspectObject?.InvokeAction(address, name);

    public void InspectRegion(nint address, uint size, string typeName, uint typeTemplateId, uint classKindId, string? name = null)
        => _inspectRegion?.InvokeAction(address, size, typeName, typeTemplateId, classKindId, name);

    public void DrawTooltipDetails(nint address)
        => _imGuiDrawPointerTooltipDetails?.InvokeAction(address);

    public void OpenContextMenu(nint address, Func<string?>? name = null)
        => _imGuiOpenPointerContextMenu?.InvokeAction(address, name);

    public (string Name, Type? BestManagedType, uint EstimatedSize, uint Displacement) GetClass(nint address)
        => _getClass?.InvokeFunc(address) ?? ("Unavailable", null, 0, 0);

    public (bool IsInstance, uint Displacement) IsInstanceOf(nint address, string? className, Type? classType)
        => _isInstanceOf?.InvokeFunc(address, className, classType) ?? (false, 0);

    public void DrawPointer(nint address, Func<string?>? name = null, string? customText = null, DrawPointerFlags flags = DrawPointerFlags.None,
        ImGuiSelectableFlags selectableFlags = ImGuiSelectableFlags.None, Vector2 size = default)
    {
        if(_drawPointerAction is not null)
        {
            _drawPointerAction.Invoke(address, name, customText, unchecked((uint)selectableFlags | ((ulong)flags << 32)), size);
        }
        else
        {
            using(ImRaii.PushFont(UiBuilder.MonoFont,
                       customText is null ? address != nint.Zero : flags.HasFlag(DrawPointerFlags.MonoFont)))
            {
                using var style = ImRaii.PushStyle(ImGuiStyleVar.Alpha, ImGui.GetStyle().Alpha * 0.5f,
                    customText is null ? address == nint.Zero : flags.HasFlag(DrawPointerFlags.Semitransparent));
                style.Push(ImGuiStyleVar.SelectableTextAlign, new Vector2(1.0f, 0.5f),
                    size != default || flags.HasFlag(DrawPointerFlags.RightAligned));
                if(ImGui.Selectable(customText ?? (address == nint.Zero ? "nullptr" : $"0x{address:X}"),
                        flags.HasFlag(DrawPointerFlags.Selected), selectableFlags, size))
                {
                    try
                    {
                        ImGui.SetClipboardText($"0x{address:X}");
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }

            if(ImGui.IsItemHovered())
            {
                using var disabled = ImRaii.Enabled();
                using var tt = ImRaii.Tooltip();
                ImGui.TextUnformatted("Click to copy to clipboard."u8);
            }
        }
    }

    public unsafe void DrawPointer(void* address, Func<string?>? name = null, string? customText = null,
        DrawPointerFlags flags = DrawPointerFlags.None, ImGuiSelectableFlags selectableFlags = ImGuiSelectableFlags.None,
        Vector2 size = default)
        => DrawPointer((nint)address, name, customText, flags, selectableFlags, size);


    //

    private void OnInitialized(uint major, uint minor, ulong flags, Version _)
    {
        OnDisposed();

        if(major is not 1)
        {
            Brio.Log.Debug($"Could not attach to Dynamis {VersionMajor}.{VersionMinor}, only 1.X is supported.");
            return;
        }

        if(minor < 6)
        {
            Brio.Log.Debug($"Could not attach to Dynamis {VersionMajor}.{VersionMinor}, only 1.6 or higher is supported.");
            return;
        }

        VersionMajor = major;
        VersionMinor = minor;
        Features = flags;

        try
        {
            _inspectObject = _pluginInterface.GetIpcSubscriber<nint, string?, object?>("Dynamis.InspectObject.V2");
            _inspectRegion = _pluginInterface.GetIpcSubscriber<nint, uint, string, uint, uint, string?, object?>("Dynamis.InspectRegion.V2");
            _getClass = _pluginInterface.GetIpcSubscriber<nint, (string, Type?, uint, uint)>("Dynamis.GetClass.V1");
            _isInstanceOf = _pluginInterface.GetIpcSubscriber<nint, string?, Type?, (bool, uint)>("Dynamis.IsInstanceOf.V1");
            _preloadDataYaml = _pluginInterface.GetIpcSubscriber<object?>("Dynamis.PreloadDataYaml.V1");

            _imGuiDrawPointerTooltipDetails = _pluginInterface.GetIpcSubscriber<nint, object?>("Dynamis.ImGuiDrawPointerTooltipDetails.V1");
            _imGuiOpenPointerContextMenu = _pluginInterface.GetIpcSubscriber<nint, Func<string?>?, object?>("Dynamis.ImGuiOpenPointerContextMenu.V1");

            _drawPointerAction = _pluginInterface.GetIpcSubscriber<Action<nint, Func<string?>?, string?, ulong, Vector2>>("Dynamis.GetImGuiDrawPointerDelegate.V3").InvokeFunc();

            _preloadDataYaml.InvokeAction();

            Brio.Log.Debug($"Attached to Dynamis {VersionMajor}.{VersionMinor}.");
        }
        catch(Exception ex)
        {
            Error = ex;
            Brio.Log.Error($"Error subscribing to Dynamis IPC:\n{ex}");
            OnDisposed();
        }
    }

    private void OnDisposed()
    {
        if(IsAttached)
            Brio.Log.Information($"Detaching from Dynamis! {VersionMajor}.{VersionMinor}.");

        Error = null;
        VersionMajor = 0;
        VersionMinor = 0;
        Features = 0;

        _inspectObject = null;
        _inspectRegion = null;
        _getClass = null;
        _isInstanceOf = null;
        _preloadDataYaml = null;

        _imGuiDrawPointerTooltipDetails = null;
        _imGuiOpenPointerContextMenu = null;

        _drawPointerAction = null;
    }

    public override void Dispose()
    {
        OnDisposed();

        _initialized.Unsubscribe(OnInitialized);
        _disposed.Unsubscribe(OnDisposed);
    }

    // From https://github.com/Exter-N/Dynamis/blob/main/Dynamis/UI/ImGuiComponents.cs
    //

    [Flags]
    public enum DrawPointerFlags : uint
    {
        None = 0,

        /// <summary>
        /// Draws the ImGui selectable as selected.
        /// </summary>
        Selected = 1,

        /// <summary>
        /// Draws the supplied custom text in a monospace font.
        /// Applied to the default text if the pointer is not null.
        /// </summary>
        MonoFont = 2,

        /// <summary>
        /// Draws the supplied custom text with halved opacity.
        /// Applied to the default text if the pointer is null.
        /// </summary>
        Semitransparent = 4,

        /// <summary>
        /// Aligns the text to the right horizontally and centers it vertically.
        /// Always applied when passed an explicit size.
        /// </summary>
        RightAligned = 8,
    }
}

//
//
//                                 Apache License
//                            Version 2.0, January 2004
//                         http://www.apache.org/licenses/
// 
//    TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION
// 
//    1. Definitions.
// 
//       "License" shall mean the terms and conditions for use, reproduction,
//       and distribution as defined by Sections 1 through 9 of this document.
// 
//       "Licensor" shall mean the copyright owner or entity authorized by
//       the copyright owner that is granting the License.
// 
//       "Legal Entity" shall mean the union of the acting entity and all
//       other entities that control, are controlled by, or are under common
//       control with that entity. For the purposes of this definition,
//       "control" means (i) the power, direct or indirect, to cause the
//       direction or management of such entity, whether by contract or
//       otherwise, or (ii) ownership of fifty percent (50%) or more of the
//       outstanding shares, or (iii) beneficial ownership of such entity.
// 
//       "You" (or "Your") shall mean an individual or Legal Entity
//       exercising permissions granted by this License.
// 
//       "Source" form shall mean the preferred form for making modifications,
//       including but not limited to software source code, documentation
//       source, and configuration files.
// 
//       "Object" form shall mean any form resulting from mechanical
//       transformation or translation of a Source form, including but
//       not limited to compiled object code, generated documentation,
//       and conversions to other media types.
// 
//       "Work" shall mean the work of authorship, whether in Source or
//       Object form, made available under the License, as indicated by a
//       copyright notice that is included in or attached to the work
//       (an example is provided in the Appendix below).
// 
//       "Derivative Works" shall mean any work, whether in Source or Object
//       form, that is based on (or derived from) the Work and for which the
//       editorial revisions, annotations, elaborations, or other modifications
//       represent, as a whole, an original work of authorship. For the purposes
//       of this License, Derivative Works shall not include works that remain
//       separable from, or merely link (or bind by name) to the interfaces of,
//       the Work and Derivative Works thereof.
// 
//       "Contribution" shall mean any work of authorship, including
//       the original version of the Work and any modifications or additions
//       to that Work or Derivative Works thereof, that is intentionally
//       submitted to Licensor for inclusion in the Work by the copyright owner
//       or by an individual or Legal Entity authorized to submit on behalf of
//       the copyright owner. For the purposes of this definition, "submitted"
//       means any form of electronic, verbal, or written communication sent
//       to the Licensor or its representatives, including but not limited to
//       communication on electronic mailing lists, source code control systems,
//       and issue tracking systems that are managed by, or on behalf of, the
//       Licensor for the purpose of discussing and improving the Work, but
//       excluding communication that is conspicuously marked or otherwise
//       designated in writing by the copyright owner as "Not a Contribution."
// 
//       "Contributor" shall mean Licensor and any individual or Legal Entity
//       on behalf of whom a Contribution has been received by Licensor and
//       subsequently incorporated within the Work.
// 
//    2. Grant of Copyright License. Subject to the terms and conditions of
//       this License, each Contributor hereby grants to You a perpetual,
//       worldwide, non-exclusive, no-charge, royalty-free, irrevocable
//       copyright license to reproduce, prepare Derivative Works of,
//       publicly display, publicly perform, sublicense, and distribute the
//       Work and such Derivative Works in Source or Object form.
// 
//    3. Grant of Patent License. Subject to the terms and conditions of
//       this License, each Contributor hereby grants to You a perpetual,
//       worldwide, non-exclusive, no-charge, royalty-free, irrevocable
//       (except as stated in this section) patent license to make, have made,
//       use, offer to sell, sell, import, and otherwise transfer the Work,
//       where such license applies only to those patent claims licensable
//       by such Contributor that are necessarily infringed by their
//       Contribution(s) alone or by combination of their Contribution(s)
//       with the Work to which such Contribution(s) was submitted. If You
//       institute patent litigation against any entity (including a
//       cross-claim or counterclaim in a lawsuit) alleging that the Work
//       or a Contribution incorporated within the Work constitutes direct
//       or contributory patent infringement, then any patent licenses
//       granted to You under this License for that Work shall terminate
//       as of the date such litigation is filed.
// 
//    4. Redistribution. You may reproduce and distribute copies of the
//       Work or Derivative Works thereof in any medium, with or without
//       modifications, and in Source or Object form, provided that You
//       meet the following conditions:
// 
//       (a) You must give any other recipients of the Work or
//           Derivative Works a copy of this License; and
// 
//       (b) You must cause any modified files to carry prominent notices
//           stating that You changed the files; and
// 
//       (c) You must retain, in the Source form of any Derivative Works
//           that You distribute, all copyright, patent, trademark, and
//           attribution notices from the Source form of the Work,
//           excluding those notices that do not pertain to any part of
//           the Derivative Works; and
// 
//       (d) If the Work includes a "NOTICE" text file as part of its
//           distribution, then any Derivative Works that You distribute must
//           include a readable copy of the attribution notices contained
//           within such NOTICE file, excluding those notices that do not
//           pertain to any part of the Derivative Works, in at least one
//           of the following places: within a NOTICE text file distributed
//           as part of the Derivative Works; within the Source form or
//           documentation, if provided along with the Derivative Works; or,
//           within a display generated by the Derivative Works, if and
//           wherever such third-party notices normally appear. The contents
//           of the NOTICE file are for informational purposes only and
//           do not modify the License. You may add Your own attribution
//           notices within Derivative Works that You distribute, alongside
//           or as an addendum to the NOTICE text from the Work, provided
//           that such additional attribution notices cannot be construed
//           as modifying the License.
// 
//       You may add Your own copyright statement to Your modifications and
//       may provide additional or different license terms and conditions
//       for use, reproduction, or distribution of Your modifications, or
//       for any such Derivative Works as a whole, provided Your use,
//       reproduction, and distribution of the Work otherwise complies with
//       the conditions stated in this License.
// 
//    5.Submission of Contributions.Unless You explicitly state otherwise,
//       any Contribution intentionally submitted for inclusion in the Work
//       by You to the Licensor shall be under the terms and conditions of
//       this License, without any additional terms or conditions.
//       Notwithstanding the above, nothing herein shall supersede or modify
//       the terms of any separate license agreement you may have executed
//       with Licensor regarding such Contributions.
// 
//    6.Trademarks.This License does not grant permission to use the trade
//       names, trademarks, service marks, or product names of the Licensor,
//       except as required for reasonable and customary use in describing the
//       origin of the Work and reproducing the content of the NOTICE file.
// 
//    7.Disclaimer of Warranty.Unless required by applicable law or
//       agreed to in writing, Licensor provides the Work(and each
//       Contributor provides its Contributions) on an "AS IS" BASIS,
//       WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
//       implied, including, without limitation, any warranties or conditions
//       of TITLE, NON - INFRINGEMENT, MERCHANTABILITY, or FITNESS FOR A
//       PARTICULAR PURPOSE.You are solely responsible for determining the
//       appropriateness of using or redistributing the Work and assume any
//       risks associated with Your exercise of permissions under this License.
// 
//    8.Limitation of Liability. In no event and under no legal theory,
//       whether in tort(including negligence), contract, or otherwise,
//       unless required by applicable law(such as deliberate and grossly
//       negligent acts) or agreed to in writing, shall any Contributor be
//       liable to You for damages, including any direct, indirect, special,
//       incidental, or consequential damages of any character arising as a
//       result of this License or out of the use or inability to use the
//       Work(including but not limited to damages for loss of goodwill,
//       work stoppage, computer failure or malfunction, or any and all
//       other commercial damages or losses), even if such Contributor
//       has been advised of the possibility of such damages.
// 
//    9.Accepting Warranty or Additional Liability. While redistributing
//       the Work or Derivative Works thereof, You may choose to offer,
//       and charge a fee for, acceptance of support, warranty, indemnity,
//       or other liability obligations and / or rights consistent with this
//       License.However, in accepting such obligations, You may act only
//       on Your own behalf and on Your sole responsibility, not on behalf
//       of any other Contributor, and only if You agree to indemnify,
//       defend, and hold each Contributor harmless for any liability
//       incurred by, or claims asserted against, such Contributor by reason
//       of your accepting any such warranty or additional liability.
// 
//    END OF TERMS AND CONDITIONS
// 
//    APPENDIX: How to apply the Apache License to your work.
// 
//       To apply the Apache License to your work, attach the following
//       boilerplate notice, with the fields enclosed by brackets "[]"
//       replaced with your own identifying information. (Don't include
//       the brackets!)  The text should be enclosed in the appropriate
//       comment syntax for the file format.We also recommend that a
//       file or class name and description of purpose be included on the
//       same "printed page" as the copyright notice for easier
//       identification within third-party archives.
// 
//    Copyright [yyyy] [name of copyright owner]
// 
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    
//    You may obtain a copy of the License at
//    
//        http://www.apache.org/licenses/LICENSE-2.0
//    
//    
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    
//    See the License for the specific language governing permissions and
//    
//    limitations under the License.
// 
