using Brio.Capabilities.Posing;
using Brio.Game.Posing;
using Brio.UI.Controls.Stateless;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using System;
using System.Numerics;

namespace Brio.UI.Controls.Editors;

public class BoneIKEditor
{
    public static void Draw(BonePoseInfo poseInfo, PosingCapability posing)
    {
        bool didChange = false;

        var ik = poseInfo.DefaultIK;

        // Set IK Button


        using(ImRaii.Disabled(posing?.SkeletonPosing.PoseInfo.HasIKStacks is false))
        using(ImRaii.PushFont(UiBuilder.IconFont))
            if(ImGui.Button($"{FontAwesomeIcon.BreadSlice.ToIconString()}###clear_ik", new Vector2(-1, 26)))
                posing?.SkeletonPosing.ResetIK();
        ImBrio.AttachToolTip($"Bake IK Changes{(!posing?.SkeletonPosing.PoseInfo.HasIKStacks ?? false ? ".\n\nAfter enabling IK & have made a change with IK use this to...\nBake('Lock in') all IK changes into the pose using this button." : "")}");

        var center = ImGui.GetItemRectMin() + (ImGui.GetItemRectSize() / 2);
        var radius = MathF.Ceiling(ImGui.GetTextLineHeight() * 0.9f);
        var thickness = MathF.Ceiling(ImGui.GetTextLineHeight() * 0.1f);

        if(posing?.SkeletonPosing.PoseInfo.HasIKStacks is false)
        {
            thickness += 0.2f;
            var offset = (radius - thickness) / MathF.Sqrt(2.0f);
            var lineStart = center + new Vector2(-offset, -offset);
            var lineEnd = center + new Vector2(offset, offset);
            ImGui.GetWindowDrawList().AddLine(lineStart, lineEnd, 0x400000FF, thickness);
        }

        if(ImGui.Checkbox("Enabled", ref ik.Enabled))
        {
            didChange |= true;
        }

        using(ImRaii.Disabled(!ik.Enabled))
        {
            if(ImGui.Checkbox("Enforce Constraints", ref ik.EnforceConstraints))
            {
                didChange |= true;
            }

            string solverType = ik.SolverOptions.Match(_ => "CCD", _ => "Two Joint");
            using(var combo = ImRaii.Combo("Solver", solverType))
            {
                if(combo.Success)
                {
                    if(ImGui.Selectable("CCD"))
                    {
                        ik.SolverOptions = BoneIKInfo.CalculateDefault(poseInfo.Name, false).SolverOptions;
                        didChange |= true;
                    }

                    if(BoneIKInfo.CanUseJoint(poseInfo.Name))
                    {
                        if(ImGui.Selectable("Two Joint"))
                        {
                            ik.SolverOptions = BoneIKInfo.CalculateDefault(poseInfo.Name, true).SolverOptions;
                            didChange |= true;
                        }
                    }
                }
            }

            ik.SolverOptions.Switch(
                ccd =>
                {
                    if(ImGui.SliderInt("Depth", ref ccd.Depth, 1, 20))
                    {
                        ik.SolverOptions = ccd;
                        didChange |= true;
                    }

                    if(ImGui.SliderInt("Iterations", ref ccd.Iterations, 1, 20))
                    {
                        ik.SolverOptions = ccd;
                        didChange |= true;
                    }
                },
                twoJoint =>
                {
                }
             );
        }

        if(didChange)
        {
            poseInfo.DefaultIK = ik;
        }
    }
}
