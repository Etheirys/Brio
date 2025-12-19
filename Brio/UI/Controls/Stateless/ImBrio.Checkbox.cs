
//
// From OtterGui (https://github.com/Ottermandias/OtterGui) list under Apache 2.0 License
//

//
// Modifications are to make it compatible with Brio
// 

using Dalamud.Bindings.ImGui;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Brio.UI.Controls.Stateless;

public static partial class ImBrio
{
    public class TristateCheckbox(uint crossColor = 0xFF0000FF, uint checkColor = 0xFF00FF00, uint dotColor = 0xFFD0D0D0) : MultiStateCheckbox<sbyte>
    {
        public readonly uint CrossColor = MergeAlpha(crossColor);
        public readonly uint CheckColor = MergeAlpha(checkColor);
        public readonly uint DotColor = MergeAlpha(dotColor);

        private static uint MergeAlpha(uint color)
            => (color & 0x00FFFFFF) | ((uint)((color >> 24) * ImGui.GetStyle().Alpha) << 24);

        protected override void RenderSymbol(sbyte value, Vector2 position, float size)
        {
            switch(value)
            {
                case -1:
                    RenderCross(ImGui.GetWindowDrawList(), position, CrossColor, size);
                    break;
                case 1:
                    RenderCheckmark(ImGui.GetWindowDrawList(), position, CheckColor, size);
                    break;
                default:
                    RenderDot(ImGui.GetWindowDrawList(), position, DotColor, size);
                    break;
            }
        }

        protected override sbyte NextValue(sbyte value)
            => value switch
            {
                0 => 1,
                1 => -1,
                _ => 0,
            };

        protected override sbyte PreviousValue(sbyte value)
            => value switch
            {
                0 => -1,
                1 => 0,
                _ => 1,
            };
    }

    /// <summary>
    /// Draw a checkbox that toggles forward or backward between different states.
    /// </summary>
    public abstract class MultiStateCheckbox<T>
    {
        /// <summary> Render the symbol corresponding to <paramref name="value"/> starting at <paramref name="position"/> and with <paramref name="size"/> as box size. </summary>
        protected abstract void RenderSymbol(T value, Vector2 position, float size);

        /// <summary> Increment the value. </summary>
        protected abstract T NextValue(T value);

        /// <summary> Decrement the value. </summary>
        protected abstract T PreviousValue(T value);

        /// <summary> Draw the multi state checkbox. </summary>
        /// <param name="label"> The label for the checkbox. </param>
        /// <param name="value"> The input/output value. </param>
        /// <returns> True when <paramref name="value"/> changed in this frame. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Draw(string label, ref T value)
            => Draw(label, value, out value);

        /// <summary> Draw the multi state checkbox. </summary>
        /// <param name="label"> The label for the checkbox. </param>
        /// <param name="currentValue"> The input value. </param>
        /// <param name="newValue"> The output value. </param>
        /// <returns> True when this was toggled this frame and a new value is returned. </returns>
        public bool Draw(string label, T currentValue, out T newValue)
        {
            newValue = currentValue;

            // Calculate dimensions
            var squareSize = ImGui.GetFrameHeight();
            var style = ImGui.GetStyle();
            var labelSize = ImGui.CalcTextSize(label);
            var screenPos = ImGui.GetCursorScreenPos();
            var itemSize = new Vector2(squareSize + (labelSize.X > 0 ? style.ItemInnerSpacing.X + labelSize.X : 0), squareSize);

            // Draw the checkbox frame
            var checkBoundingBox = new Vector2(squareSize);
            var frameColor = ImGui.GetColorU32(ImGui.IsItemHovered() ? ImGuiCol.FrameBgHovered : ImGuiCol.FrameBg);

            ImGui.PushID(label);
            var clicked = ImGui.InvisibleButton("##checkbox", itemSize);
            var rightClick = ImGui.IsItemClicked(ImGuiMouseButton.Right);
            var hovered = ImGui.IsItemHovered();
            ImGui.PopID();

            // Handle user interaction
            if(rightClick)
                newValue = PreviousValue(currentValue);
            else if(clicked)
                newValue = NextValue(currentValue);

            // Draw the checkbox background
            var drawList = ImGui.GetWindowDrawList();
            var frameBgColor = ImGui.GetColorU32(hovered ? ImGuiCol.FrameBgHovered : ImGuiCol.FrameBg);
            drawList.AddRectFilled(screenPos, screenPos + checkBoundingBox, frameBgColor, style.FrameRounding);
            drawList.AddRect(screenPos, screenPos + checkBoundingBox, ImGui.GetColorU32(ImGuiCol.Border), style.FrameRounding);

            // Draw the desired symbol into the checkbox
            var paddingSize = Math.Max(1, (int)(squareSize / 6));
            RenderSymbol(currentValue, screenPos + new Vector2(paddingSize), squareSize - paddingSize * 2);

            // Add the label if there is one visible
            if(labelSize.X > 0)
            {
                var labelPos = screenPos + new Vector2(squareSize + style.ItemInnerSpacing.X, style.FramePadding.Y);
                drawList.AddText(labelPos, ImGui.GetColorU32(ImGuiCol.Text), label);
            }

            return clicked || rightClick;
        }
    }

    /// <summary> Render a simple cross (X) in a square. </summary>
    /// <param name="drawList"> The draw list to render in. </param>
    /// <param name="position"> The upper left corner of the square. </param>
    /// <param name="color"> The color of the cross. </param>
    /// <param name="size"> The size of the square. </param>
    public static void RenderCross(ImDrawListPtr drawList, Vector2 position, uint color, float size)
    {
        var offset = (int)size & 1;
        var thickness = Math.Max(size / 5, 1);
        var padding = new Vector2(thickness / 3f);
        size -= padding.X * 2 + offset;
        position += padding;
        var otherCorner = position + new Vector2(size);
        drawList.AddLine(position, otherCorner, color, thickness);
        position.X += size;
        otherCorner.X -= size;
        drawList.AddLine(position, otherCorner, color, thickness);
    }

    /// <summary> Render a simple checkmark in a square. </summary>
    /// <param name="drawList"> The draw list to render in. </param>
    /// <param name="position"> The upper left corner of the square. </param>
    /// <param name="color"> The color of the checkmark. </param>
    /// <param name="size"> The size of the square. </param>
    public static void RenderCheckmark(ImDrawListPtr drawList, Vector2 position, uint color, float size)
    {
        var thickness = Math.Max(size / 5, 1);
        size -= thickness / 2;
        var padding = new Vector2(thickness / 4);
        position += padding;

        var third = size / 3;
        var bx = position.X + third;
        var by = position.Y + size - third / 2;
        drawList.PathLineTo(new Vector2(bx - third, by - third));
        drawList.PathLineTo(new Vector2(bx, by));
        drawList.PathLineTo(new Vector2(bx + third * 2.0f, by - third * 2.0f));
        drawList.PathStroke(color, 0, thickness);
    }

    /// <summary> Render a simple dot in a square. </summary>
    /// <param name="drawList"> The draw list to render in. </param>
    /// <param name="position"> The upper left corner of the square. </param>
    /// <param name="color"> The color of the dot. </param>
    /// <param name="size"> The size of the square. </param>
    public static void RenderDot(ImDrawListPtr drawList, Vector2 position, uint color, float size)
    {
        var padding = size / 7;
        var pos = position + new Vector2(size / 2);
        size = size / 2 - padding;
        drawList.AddCircleFilled(pos, size, color);
    }

    /// <summary> Render a simple dash in a square. </summary>
    /// <param name="drawList"> The draw list to render in. </param>
    /// <param name="position"> The upper left corner of the square. </param>
    /// <param name="color"> The color of the dash. </param>
    /// <param name="size"> The size of the square. </param>
    public static void RenderDash(ImDrawListPtr drawList, Vector2 position, uint color, float size)
    {
        var offset = (int)size & 1;
        var thickness = (int)Math.Max(size / 4, 1) | offset;
        var padding = thickness / 2;
        position.X += padding;
        position.Y += size / 2;
        size -= padding * 2;

        var otherCorner = position with { X = position.X + size };
        drawList.AddLine(position, otherCorner, color, thickness);
    }
}

// --------------------------------------------------------------------
//
//                                  Apache License
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
//    5. Submission of Contributions. Unless You explicitly state otherwise,
//       any Contribution intentionally submitted for inclusion in the Work
//       by You to the Licensor shall be under the terms and conditions of
//       this License, without any additional terms or conditions.
//       Notwithstanding the above, nothing herein shall supersede or modify
//       the terms of any separate license agreement you may have executed
//       with Licensor regarding such Contributions.
//
//    6. Trademarks. This License does not grant permission to use the trade
//       names, trademarks, service marks, or product names of the Licensor,
//       except as required for reasonable and customary use in describing the
//       origin of the Work and reproducing the content of the NOTICE file.
//
//    7. Disclaimer of Warranty. Unless required by applicable law or
//       agreed to in writing, Licensor provides the Work (and each
//       Contributor provides its Contributions) on an "AS IS" BASIS,
//       WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
//       implied, including, without limitation, any warranties or conditions
//       of TITLE, NON-INFRINGEMENT, MERCHANTABILITY, or FITNESS FOR A
//       PARTICULAR PURPOSE. You are solely responsible for determining the
//       appropriateness of using or redistributing the Work and assume any
//       risks associated with Your exercise of permissions under this License.
//
//    8. Limitation of Liability. In no event and under no legal theory,
//       whether in tort (including negligence), contract, or otherwise,
//       unless required by applicable law (such as deliberate and grossly
//       negligent acts) or agreed to in writing, shall any Contributor be
//       liable to You for damages, including any direct, indirect, special,
//       incidental, or consequential damages of any character arising as a
//       result of this License or out of the use or inability to use the
//       Work (including but not limited to damages for loss of goodwill,
//       work stoppage, computer failure or malfunction, or any and all
//       other commercial damages or losses), even if such Contributor
//       has been advised of the possibility of such damages.
//
//    9. Accepting Warranty or Additional Liability. While redistributing
//       the Work or Derivative Works thereof, You may choose to offer,
//       and charge a fee for, acceptance of support, warranty, indemnity,
//       or other liability obligations and/or rights consistent with this
//       License. However, in accepting such obligations, You may act only
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
//       comment syntax for the file format. We also recommend that a
//       file or class name and description of purpose be included on the
//       same "printed page" as the copyright notice for easier
//       identification within third-party archives.
//
//    Copyright [yyyy] [name of copyright owner]
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
//
// --------------------------------------------------------------------
