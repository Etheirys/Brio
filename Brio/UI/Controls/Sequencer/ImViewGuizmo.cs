// ImViewGuizmo.cs - C# port from ImViewGuizmo.h
// Uses System.Numerics and Hexa.NET.ImGui
// Ported from https://github.com/Ka1serM/ImViewGuizmo/blob/104a4e1cf20e87fff1daa264690e0f9f446920e1/ImViewGuizmo.h

using System;
using System.Numerics;
using System.Collections.Generic;
using Dalamud.Bindings.ImGui;

namespace ImViewGuizmo
{
    public static class ImViewGuizmo
    {
        // --- INTERFACE ---

        public class Style
        {
            public float Scale = 1f;

            // Axis visuals
            public float LineLength = 0.5f;
            public float LineWidth = 4.0f;
            public float CircleRadius = 15.0f;
            public float FadeFactor = 0.25f;

            // Highlight
            public uint HighlightColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 0f, 1f));
            public float HighlightWidth = 2.0f;

            // Axis
            public uint[] AxisColors = new uint[3]
            {
                ImGui.ColorConvertFloat4ToU32(new Vector4(230/255f, 51/255f, 51/255f, 1f)),   // X
                ImGui.ColorConvertFloat4ToU32(new Vector4(51/255f, 230/255f, 51/255f, 1f)),   // Y
                ImGui.ColorConvertFloat4ToU32(new Vector4(51/255f, 128/255f, 1f, 1f))         // Z
            };

            // Labels
            public float LabelSize = 1.0f;
            public string[] AxisLabels = new string[3] { "X", "Y", "Z" };
            public uint LabelColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 1f));

            // Big Circle
            public float BigCircleRadius = 80.0f;
            public uint BigCircleColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 0.196f));

            // Animation
            public bool AnimateSnap = true;
            public float SnapAnimationDuration = 0.3f; // seconds

            // Zoom/Pan Button Visuals
            public float ToolButtonRadius = 25f;
            public float ToolButtonInnerPadding = 4f;
            public uint ToolButtonColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.565f, 0.565f, 0.565f, 0.196f));
            public uint ToolButtonHoveredColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.843f, 0.843f, 0.843f, 0.196f));
            public uint ToolButtonIconColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.843f, 0.843f, 0.843f, 0.882f));
        }

        private static Style _style = new Style();
        public static ref Style GetStyle() => ref _style;

        public struct GizmoAxis
        {
            public int Id;
            public int AxisIndex;
            public float Depth;
            public Vector3 Direction;
        }

        public enum ActiveTool
        {
            None,
            Gizmo,
            Zoom,
            Pan
        }

        public const float BaseSize = 256f;
        public static readonly Vector3 Origin = Vector3.Zero;
        public static readonly Vector3 WorldRight = new(1f, 0f, 0f);
        public static readonly Vector3 WorldUp = new(0f, -1f, 0f);
        public static readonly Vector3 WorldForward = new(0f, 0f, 1f);
        public static readonly Vector3[] AxisVectors = new[]
        {
            new Vector3(1,0,0),
            new Vector3(0,1,0),
            new Vector3(0,0,1)
        };

        public class Context
        {
            public int HoveredAxisId = -1;
            public bool IsZoomButtonHovered = false;
            public bool IsPanButtonHovered = false;
            public ActiveTool ActiveTool = ActiveTool.None;

            // Animation state
            public bool IsAnimating = false;
            public float AnimationStartTime = 0f;
            public Vector3 StartPos;
            public Vector3 TargetPos;
            public Vector3 StartUp;
            public Vector3 TargetUp;

            public bool IsHoveringGizmo() => HoveredAxisId != -1;
            public void Reset()
            {
                HoveredAxisId = -1;
                IsZoomButtonHovered = false;
                IsPanButtonHovered = false;
                ActiveTool = ActiveTool.None;
            }
        }

        private static Context _context = new();
        public static ref Context GetContext() => ref _context;

        public static bool IsUsing() => GetContext().ActiveTool != ActiveTool.None;

        public static bool IsOver()
        {
            var ctx = GetContext();
            return ctx.HoveredAxisId != -1 || ctx.IsZoomButtonHovered || ctx.IsPanButtonHovered;
        }

        // --- Math Helpers ---

        public static float Mix(float a, float b, float t) => a * (1f - t) + b * t;
        public static Vector3 Mix(Vector3 a, Vector3 b, float t) => a * (1f - t) + b * t;

        
        // replace with https://stackoverflow.com/questions/12435671/quaternion-lookat-function/51170230#51170230
        public static Quaternion QuaternionLookAt(Vector3 forward, Vector3 up)
        {
            var z = Vector3.Normalize(forward);
            var x = Vector3.Normalize(Vector3.Cross(up, z));
            var y = Vector3.Cross(z, x);

            float m00 = x.X, m01 = y.X, m02 = z.X;
            float m10 = x.Y, m11 = y.Y, m12 = z.Y;
            float m20 = x.Z, m21 = y.Z, m22 = z.Z;

            float num8 = (m00 + m11) + m22;
            Quaternion q = new();
            if (num8 > 0f)
            {
                float num = MathF.Sqrt(num8 + 1f);
                q.W = num * 0.5f;
                num = 0.5f / num;
                q.X = (m12 - m21) * num;
                q.Y = (m20 - m02) * num;
                q.Z = (m01 - m10) * num;
                return q;
            }
            if ((m00 >= m11) && (m00 >= m22))
            {
                float num7 = MathF.Sqrt(((1f + m00) - m11) - m22);
                float num4 = 0.5f / num7;
                q.X = 0.5f * num7;
                q.Y = (m01 + m10) * num4;
                q.Z = (m02 + m20) * num4;
                q.W = (m12 - m21) * num4;
                return q;
            }
            if (m11 > m22)
            {
                float num6 = MathF.Sqrt(((1f + m11) - m00) - m22);
                float num3 = 0.5f / num6;
                q.X = (m10 + m01) * num3;
                q.Y = 0.5f * num6;
                q.Z = (m21 + m12) * num3;
                q.W = (m20 - m02) * num3;
                return q;
            }
            float num5 = MathF.Sqrt(((1f + m22) - m00) - m11);
            float num2 = 0.5f / num5;
            q.X = (m20 + m02) * num2;
            q.Y = (m21 + m12) * num2;
            q.Z = 0.5f * num5;
            q.W = (m01 - m10) * num2;
            return q;
        }

        // --- Main API ---

        private static int _lastFrame = -1;
        private static void BeginFrame()
        {
            int currentFrame = ImGui.GetFrameCount();
            if (_lastFrame != currentFrame)
            {
                _lastFrame = currentFrame;
                var ctx = GetContext();
                ctx.HoveredAxisId = -1;
                ctx.IsZoomButtonHovered = false;
                ctx.IsPanButtonHovered = false;
            }
        }

        public static bool Rotate(ref Vector3 cameraPos, ref Quaternion cameraRot, Vector2 position, float snapDistance = 5f, float rotationSpeed = 0.005f)
        {
            BeginFrame();
            var io = ImGui.GetIO();
            var drawList = ImGui.GetWindowDrawList();
            var ctx = GetContext();
            var style = GetStyle();
            bool wasModified = false;

            if (!ImGui.IsMouseDown(0) && ctx.ActiveTool != ActiveTool.None)
                ctx.ActiveTool = ActiveTool.None;

            // Animation logic
            if (ctx.IsAnimating)
            {
                float elapsedTime = (float)ImGui.GetTime() - ctx.AnimationStartTime;
                float t = MathF.Min(1.0f, elapsedTime / style.SnapAnimationDuration);
                t = 1.0f - (1.0f - t) * (1.0f - t);

                Vector3 currentDir = ctx.StartPos.Length() > 0.0001f && ctx.TargetPos.Length() > 0.0001f
                    ? Vector3.Normalize(Mix(Vector3.Normalize(ctx.StartPos), Vector3.Normalize(ctx.TargetPos), t))
                    : Vector3.Normalize(Mix(new Vector3(0, 0, 1), Vector3.Normalize(ctx.TargetPos), t));
                float startDistance = ctx.StartPos.Length();
                float targetDistance = ctx.TargetPos.Length();
                float currentDistance = Mix(startDistance, targetDistance, t);
                cameraPos = currentDir * currentDistance;

                Vector3 currentUp = Vector3.Normalize(Mix(ctx.StartUp, ctx.TargetUp, t));
                cameraRot = QuaternionLookAt(Vector3.Normalize(cameraPos), currentUp);

                wasModified = true;

                if (t >= 1.0f)
                {
                    cameraPos = ctx.TargetPos;
                    cameraRot = QuaternionLookAt(Vector3.Normalize(ctx.TargetPos), ctx.TargetUp);
                    ctx.IsAnimating = false;
                }
            }

            float gizmoDiameter = BaseSize * style.Scale;
            float scaledCircleRadius = style.CircleRadius * style.Scale;
            float scaledBigCircleRadius = style.BigCircleRadius * style.Scale;
            float scaledLineWidth = style.LineWidth * style.Scale;
            float scaledHighlightWidth = style.HighlightWidth * style.Scale;
            float scaledHighlightRadius = (style.CircleRadius + 2.0f) * style.Scale;
            float scaledFontSize = ImGui.GetFontSize() * style.Scale * style.LabelSize;

            // Matrices
            Matrix4x4 worldMatrix = Matrix4x4.CreateFromQuaternion(cameraRot) * Matrix4x4.CreateTranslation(cameraPos);
            Matrix4x4 viewMatrix = Matrix4x4.Invert(worldMatrix, out var inv) ? inv : Matrix4x4.Identity;
            Matrix4x4 gizmoViewMatrix = new(
                viewMatrix.M11, viewMatrix.M12, viewMatrix.M13, 0,
                viewMatrix.M21, viewMatrix.M22, viewMatrix.M23, 0,
                viewMatrix.M31, viewMatrix.M32, viewMatrix.M33, 0,
                0, 0, 0, 1
            );
            Matrix4x4 gizmoProjectionMatrix = Matrix4x4.CreateOrthographicOffCenter(-1f, 1f, -1f, 1f, -100f, 100f);
            Matrix4x4 gizmoMvp = gizmoViewMatrix * gizmoProjectionMatrix;

            // Axes
            List<GizmoAxis> axes = new();
            for (int i = 0; i < 3; ++i)
            {
                axes.Add(new GizmoAxis { Id = i * 2, AxisIndex = i, Depth = Vector3.TransformNormal(AxisVectors[i], gizmoViewMatrix).Z, Direction = AxisVectors[i] });
                axes.Add(new GizmoAxis { Id = i * 2 + 1, AxisIndex = i, Depth = Vector3.TransformNormal(-AxisVectors[i], gizmoViewMatrix).Z, Direction = -AxisVectors[i] });
            }
            axes.Sort((a, b) => a.Depth.CompareTo(b.Depth));

            Vector2 WorldToScreen(Vector3 worldPos)
            {
                Vector4 clipPos = Vector4.Transform(new Vector4(worldPos, 1f), gizmoMvp);
                if (clipPos.W == 0.0f) return new(float.NegativeInfinity, float.NegativeInfinity);
                Vector3 ndc = new(clipPos.X, clipPos.Y, clipPos.Z);
                ndc /= clipPos.W;
                return new(
                    position.X + ndc.X * (gizmoDiameter / 2f),
                    position.Y - ndc.Y * (gizmoDiameter / 2f)
                );
            }

            // Hover logic
            if (ctx.ActiveTool == ActiveTool.None && !ctx.IsAnimating)
            {
                float halfGizmoSize = gizmoDiameter / 2f;
                var mousePos = io.MousePos;
                float distToCenterSq = (mousePos - position).LengthSquared();

                if (distToCenterSq < (halfGizmoSize + scaledCircleRadius) * (halfGizmoSize + scaledCircleRadius))
                {
                    float minDistanceSq = scaledCircleRadius * scaledCircleRadius;
                    foreach (var axis in axes)
                    {
                        if (axis.Depth < -0.1f)
                            continue;
                        var handlePos = WorldToScreen(axis.Direction * style.LineLength);
                        if ((handlePos - mousePos).LengthSquared() < minDistanceSq)
                            ctx.HoveredAxisId = axis.Id;
                    }
                    if (ctx.HoveredAxisId == -1)
                    {
                        var centerPos = WorldToScreen(Origin);
                        if ((centerPos - mousePos).LengthSquared() < scaledBigCircleRadius * scaledBigCircleRadius)
                            ctx.HoveredAxisId = 6;
                    }
                }
            }

            // Big circle
            if (ctx.HoveredAxisId == 6 || ctx.ActiveTool == ActiveTool.Gizmo)
                drawList.AddCircleFilled(WorldToScreen(Origin), scaledBigCircleRadius, style.BigCircleColor);

            // Draw axes
            foreach (var axis in axes)
            {
                float factor = Mix(style.FadeFactor, 1.0f, (axis.Depth + 1.0f) * 0.5f);
                var baseColor = ImGui.ColorConvertU32ToFloat4(style.AxisColors[axis.AxisIndex]);
                var fadedColor = new Vector4(baseColor.X, baseColor.Y, baseColor.Z, baseColor.W * factor);
                uint finalColor = ImGui.ColorConvertFloat4ToU32(fadedColor);

                var originPos = WorldToScreen(Origin);
                var handlePos = WorldToScreen(axis.Direction * style.LineLength);

                var lineDir = handlePos - originPos;
                float lineLen = lineDir.Length() + 1e-6f;
                lineDir /= lineLen;
                var lineEndPos = handlePos - lineDir * scaledCircleRadius;

                drawList.AddLine(originPos, lineEndPos, finalColor, scaledLineWidth);
                drawList.AddCircleFilled(handlePos, scaledCircleRadius, finalColor);

                if (ctx.HoveredAxisId == axis.Id)
                    drawList.AddCircle(handlePos, scaledHighlightRadius, style.HighlightColor, 0, scaledHighlightWidth);
            }

            // Draw labels
            var font = ImGui.GetFont();
            foreach (var axis in axes)
            {
                if (axis.Depth < -0.1f)
                    continue;
                var textPos = WorldToScreen(axis.Direction * style.LineLength);
                string label = style.AxisLabels[axis.AxisIndex];
                var remaining = 0;
                var textSize = ImGui.CalcTextSizeA(font, scaledFontSize, float.MaxValue, 0, label, out remaining ); //font.CalcTextSizeA(scaledFontSize, float.MaxValue, 0, label);
                drawList.AddText(font, scaledFontSize, new(textPos.X - textSize.X * 0.5f, textPos.Y - textSize.Y * 0.5f), style.LabelColor, label);
            }

            // Drag logic
            if (ImGui.IsMouseDown(0))
            {
                if (ctx.ActiveTool == ActiveTool.None && ctx.HoveredAxisId == 6)
                {
                    ctx.ActiveTool = ActiveTool.Gizmo;
                    ctx.IsAnimating = false;
                }
            }
            if (ctx.ActiveTool == ActiveTool.Gizmo)
            {
                float yawAngle = -io.MouseDelta.X * rotationSpeed;
                float pitchAngle = -io.MouseDelta.Y * rotationSpeed;
                var yawRotation = Quaternion.CreateFromAxisAngle(WorldUp, yawAngle);
                var rightAxis = Vector3.Transform(WorldRight, cameraRot);
                var pitchRotation = Quaternion.CreateFromAxisAngle(rightAxis, pitchAngle);
                var totalRotation = Quaternion.Normalize(Quaternion.Concatenate(yawRotation, pitchRotation));
                cameraPos = Vector3.Transform(cameraPos, totalRotation);
                cameraRot = Quaternion.Normalize(Quaternion.Concatenate(totalRotation, cameraRot));
                wasModified = true;
            }

            // Snap
            if (ImGui.IsMouseReleased(0) && ctx.HoveredAxisId >= 0 && ctx.HoveredAxisId <= 5 && !ImGui.IsMouseDragging(0))
            {
                int axisIndex = ctx.HoveredAxisId / 2;
                float sign = (ctx.HoveredAxisId % 2 == 0) ? -1.0f : 1.0f;
                Vector3 targetDir = sign * AxisVectors[axisIndex];
                Vector3 targetPosition = targetDir * snapDistance;

                Vector3 up = WorldUp;
                if (axisIndex == 1) up = WorldForward;
                Vector3 targetUp = -up;

                var targetRotation = QuaternionLookAt(targetDir, targetUp);

                if (style.AnimateSnap && style.SnapAnimationDuration > 0.0f)
                {
                    bool posIsDifferent = Vector3.DistanceSquared(cameraPos, targetPosition) > 0.0001f;
                    bool rotIsDifferent = (1.0f - MathF.Abs(Quaternion.Dot(cameraRot, targetRotation))) > 0.0001f;

                    if (posIsDifferent || rotIsDifferent)
                    {
                        ctx.IsAnimating = true;
                        ctx.AnimationStartTime = (float)ImGui.GetTime();
                        ctx.StartPos = cameraPos;
                        ctx.TargetPos = targetPosition;
                        ctx.StartUp = Vector3.Transform(new Vector3(0, 1, 0), cameraRot);
                        ctx.TargetUp = targetUp;
                    }
                }
                else
                {
                    cameraRot = targetRotation;
                    cameraPos = targetPosition;
                    wasModified = true;
                }
            }

            return wasModified;
        }

        public static bool Zoom(ref Vector3 cameraPos, Quaternion cameraRot, Vector2 position, float zoomSpeed = 0.005f)
        {
            BeginFrame();
            var io = ImGui.GetIO();
            var drawList = ImGui.GetWindowDrawList();
            var ctx = GetContext();
            var style = GetStyle();
            bool wasModified = false;

            float radius = style.ToolButtonRadius * style.Scale;
            var center = new Vector2(position.X + radius, position.Y + radius);

            bool isHovered = false;
            if (ctx.ActiveTool == ActiveTool.None || ctx.ActiveTool == ActiveTool.Zoom)
                if ((io.MousePos - center).LengthSquared() < radius * radius)
                    isHovered = true;

            ctx.IsZoomButtonHovered = isHovered;

            if (isHovered && ImGui.IsMouseDown(0) && ctx.ActiveTool == ActiveTool.None)
            {
                ctx.ActiveTool = ActiveTool.Zoom;
                ctx.IsAnimating = false;
            }

            if (ctx.ActiveTool == ActiveTool.Zoom)
            {
                if (io.MouseDelta.Y != 0.0f)
                {
                    Vector3 cameraForward = Vector3.Transform(WorldForward, cameraRot);
                    cameraPos += cameraForward * -io.MouseDelta.Y * zoomSpeed;
                    wasModified = true;
                }
            }

            uint bgColor = style.ToolButtonColor;
            if (ctx.ActiveTool == ActiveTool.Zoom || isHovered)
                bgColor = style.ToolButtonHoveredColor;
            drawList.AddCircleFilled(center, radius, bgColor);

            float p = style.ToolButtonInnerPadding * style.Scale;
            float th = 2.0f * style.Scale;
            uint iconColor = style.ToolButtonIconColor;

            const float iconScale = 0.5f;
            var glassCenter = new Vector2(center.X - (p / 2.0f) * iconScale, center.Y - (p / 2.0f) * iconScale);
            float glassRadius = (radius - p - 1f) * iconScale;
            drawList.AddCircle(glassCenter, glassRadius, iconColor, 0, th);

            var handleStart = new Vector2(center.X + (radius / 2.0f) * iconScale, center.Y + (radius / 2.0f) * iconScale);
            var handleEnd = new Vector2(center.X + (radius - p) * iconScale, center.Y + (radius - p) * iconScale);
            drawList.AddLine(handleStart, handleEnd, iconColor, th);

            var plusVertStart = new Vector2(center.X - (p / 2.0f) * iconScale, center.Y - (radius / 2.0f) * iconScale);
            var plusVertEnd = new Vector2(center.X - (p / 2.0f) * iconScale, center.Y + (radius / 2.0f - p) * iconScale);
            drawList.AddLine(plusVertStart, plusVertEnd, iconColor, th);

            var plusHorizStart = new Vector2(center.X + (-radius / 2.0f + p / 2.0f) * iconScale, center.Y - (p / 2.0f) * iconScale);
            var plusHorizEnd = new Vector2(center.X + (radius / 2.0f - p * 1.5f) * iconScale, center.Y - (p / 2.0f) * iconScale);
            drawList.AddLine(plusHorizStart, plusHorizEnd, iconColor, th);

            return wasModified;
        }

        public static bool Pan(ref Vector3 cameraPos, Quaternion cameraRot, Vector2 position, float panSpeed = 0.001f)
        {
            BeginFrame();
            var io = ImGui.GetIO();
            var drawList = ImGui.GetWindowDrawList();
            var ctx = GetContext();
            var style = GetStyle();
            bool wasModified = false;

            float radius = style.ToolButtonRadius * style.Scale;
            var center = new Vector2(position.X + radius, position.Y + radius);

            bool isHovered = false;
            if (ctx.ActiveTool == ActiveTool.None || ctx.ActiveTool == ActiveTool.Pan)
                if ((io.MousePos - center).LengthSquared() < radius * radius)
                    isHovered = true;
            ctx.IsPanButtonHovered = isHovered;

            if (isHovered && ImGui.IsMouseDown(0) && ctx.ActiveTool == ActiveTool.None)
            {
                ctx.ActiveTool = ActiveTool.Pan;
                ctx.IsAnimating = false;
            }

            if (ctx.ActiveTool == ActiveTool.Pan)
            {
                if (io.MouseDelta.X != 0.0f || io.MouseDelta.Y != 0.0f)
                {
                    cameraPos += Vector3.Transform(WorldRight, cameraRot) * -io.MouseDelta.X * panSpeed;
                    cameraPos += Vector3.Transform(WorldUp, cameraRot) * io.MouseDelta.Y * panSpeed;
                    wasModified = true;
                }
            }

            uint bgColor = style.ToolButtonColor;
            if (ctx.ActiveTool == ActiveTool.Pan || isHovered)
                bgColor = style.ToolButtonHoveredColor;
            drawList.AddCircleFilled(center, radius, bgColor);

            uint iconColor = style.ToolButtonIconColor;
            float th = 2.0f * style.Scale;
            float size = radius * 0.5f;
            float arm = size * 0.25f;

            // Top Arrow (^)
            var topTip = new Vector2(center.X, center.Y - size);
            drawList.AddLine(new(topTip.X - arm, topTip.Y + arm), topTip, iconColor, th);
            drawList.AddLine(new(topTip.X + arm, topTip.Y + arm), topTip, iconColor, th);
            // Bottom Arrow (v)
            var botTip = new Vector2(center.X, center.Y + size);
            drawList.AddLine(new(botTip.X - arm, botTip.Y - arm), botTip, iconColor, th);
            drawList.AddLine(new(botTip.X + arm, botTip.Y - arm), botTip, iconColor, th);
            // Left Arrow (<)
            var leftTip = new Vector2(center.X - size, center.Y);
            drawList.AddLine(new(leftTip.X + arm, leftTip.Y - arm), leftTip, iconColor, th);
            drawList.AddLine(new(leftTip.X + arm, leftTip.Y + arm), leftTip, iconColor, th);
            // Right Arrow (>)
            var rightTip = new Vector2(center.X + size, center.Y);
            drawList.AddLine(new(rightTip.X - arm, rightTip.Y - arm), rightTip, iconColor, th);
            drawList.AddLine(new(rightTip.X - arm, rightTip.Y + arm), rightTip, iconColor, th);

            return wasModified;
        }
    }
}