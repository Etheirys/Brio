
﻿using Brio.Config;
﻿using Brio.Game.Camera;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using ImGuiNET;
using ImGuizmoNET;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Brio.Core;

internal static class ImGuizmoExtensions
{
    public static bool MouseWheelManipulate(ref Matrix4x4 matrix)
    {
        if(ImGui.IsAnyMouseDown())
            return false;

        float mouseWheel = ImGui.GetIO().MouseWheel / 100;

        if(mouseWheel != 0)
        {
            bool smallIncrement = ImGui.IsKeyDown(ConfigurationService.Instance.Configuration.Interface.IncrementSmall);
            if(smallIncrement)
                mouseWheel /= 10;

            bool largeIncrement = ImGui.IsKeyDown(ConfigurationService.Instance.Configuration.Interface.IncrementLarge);
            if(largeIncrement)
                mouseWheel *= 10;

            if(ImGuizmo.IsOver(OPERATION.ROTATE_X))
            {
                matrix = Matrix4x4.CreateRotationX(mouseWheel) * matrix;
                return true;
            }
            else if(ImGuizmo.IsOver(OPERATION.ROTATE_Y))
            {
                matrix = Matrix4x4.CreateRotationY(mouseWheel) * matrix;
                return true;
            }
            else if(ImGuizmo.IsOver(OPERATION.ROTATE_Z))
            {
                matrix = Matrix4x4.CreateRotationZ(mouseWheel) * matrix;
                return true;
            }
        }

        return false;
    }
}

internal static class ImBriozmo
{
    const int numPoints = 144;
    const int axisMouseDist = 10;

    private static Style style = new();
    private static ImDrawListPtr drawList;
    private static Matrix4x4 viewMatrix;
    private static float closestAxisPointToMouseDistance;
    private static Vector2? closestAxisMousePoint = null;
    private static Axis closestMouseAxis = Axis.X;
    private static Axis dragAxis = Axis.X;
    private static Vector2? dragStartPos = null;

    public enum Axis
    {
        X,
        Y,
        Z,
    }

    public struct Style
    {
        public uint[] AxisForegroundColors = new uint[3]
        {
            0xFF33FF33,
            0xFF3333FF,
            0xFFFF3333
        };

        public uint[] AxisBackgroundColors = new uint[3]
        {
            0x1033FF33,
            0x103333FF,
            0x10FF3333
        };

        public Style()
        {
        }
    }

    public unsafe static bool DrawRotation(Matrix4x4 matrix)
    {
        drawList = ImGui.GetWindowDrawList();

        BrioCamera* camera = (BrioCamera*)CameraManager.Instance()->GetActiveCamera();
        viewMatrix = camera->GetViewMatrix();

        float radius = 50;
        int lineThickness = 2;

        // reset context
        closestAxisPointToMouseDistance = axisMouseDist;
        closestAxisMousePoint = null;

        Vector2 size = new Vector2(150, 150);
        if(ImGui.BeginChild("##imbriozmo", size))
        {
            Vector2 topPos = ImGui.GetWindowPos() + ImGui.GetWindowContentRegionMin();
            Vector2 botPos = ImGui.GetWindowPos() + ImGui.GetWindowContentRegionMax();

            Vector2 center = topPos + ((botPos - topPos) / 2);

            drawList.AddCircleFilled(center, radius, 0x50000000);

            DrawAxis(center, lineThickness, radius, Axis.X);
            DrawAxis(center, lineThickness, radius, Axis.Y);
            DrawAxis(center, lineThickness, radius, Axis.Z);

            
            // Mouse drag
            if(dragStartPos != null)
            {
                drawList.AddCircleFilled((Vector2)dragStartPos, lineThickness * 2, style.AxisForegroundColors[(int)dragAxis]);

                if(!ImGui.IsMouseDown(ImGuiMouseButton.Left))
                {
                    dragStartPos = null;
                }
            }

            // Mouse Hover
            else if(closestAxisMousePoint != null)
            {
                if(ImGui.IsMouseDown(ImGuiMouseButton.Left))
                {
                    dragStartPos = closestAxisMousePoint;
                    dragAxis = closestMouseAxis;
                }
                
                drawList.AddCircle((Vector2)closestAxisMousePoint, axisMouseDist, style.AxisForegroundColors[(int)closestMouseAxis]);
            }
        }

        ImGui.InvisibleButton("##imbriozmo_cover", size);

        return false;
    }

    private unsafe static void DrawAxis(
        Vector2 center,
        int thickness,
        float radius,
        Axis axis)
    {
        Vector3[] points3d = new Vector3[numPoints];
        for (int i = 0; i < points3d.Length; i++)
        {
            float p = (float)i / (float)(points3d.Length - 1);
            float r = p * (MathF.PI * 2);

            if (axis == Axis.X)
            {
                points3d[i] = new Vector3(radius * MathF.Cos(r), radius * MathF.Sin(r), 0);
            }
            else if(axis == Axis.Y)
            {
                points3d[i] = new Vector3(0, 50 * MathF.Cos(r), 50 * MathF.Sin(r));
            }
            else if(axis == Axis.Z)
            {
                points3d[i] = new Vector3(50 * MathF.Cos(r), 0, 50 * MathF.Sin(r));
            }
        }

        Vector2 mousePos = ImGui.GetMousePos();
       

        float zClip = 0f;
        for(int i = 1; i < points3d.Length; i++)
        {
            Vector3 fromPoint = Vector3.Transform(points3d[i - 1], viewMatrix);
            Vector3 toPoint = Vector3.Transform(points3d[i], viewMatrix);

            bool isVisible = toPoint.Z < zClip;

            Vector2 fromPos = center + new Vector2(fromPoint.X, fromPoint.Y);
            Vector2 toPos = center + new Vector2(toPoint.X, toPoint.Y);

            uint segmentColor = isVisible ? style.AxisForegroundColors[(int)axis] : style.AxisBackgroundColors[(int)axis];

            // check mouse
            if(isVisible)
            {
                float distToMouse = Vector2.Distance(toPos, mousePos);
                if(distToMouse <= closestAxisPointToMouseDistance)
                {
                    closestAxisPointToMouseDistance = distToMouse;
                    closestAxisMousePoint = toPos;
                    closestMouseAxis = axis;
                }
            }

            drawList.AddLine(fromPos, toPos, segmentColor, thickness);
        }
    }
}
