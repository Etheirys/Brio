﻿using Brio.Config;
using Brio.Game.Camera;
using Brio.Input;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using ImGuiNET;
using System;
using System.Numerics;

namespace Brio.UI.Controls.Stateless;

internal static partial class ImBrioGizmo
{
    const int numPoints = 144;
    const int axisHoverMouseDist = 10;

    private static Style style = new();
    private static ImDrawListPtr drawList;
    private static Matrix4x4 viewMatrix;
    private static Matrix4x4 transformMatrix;
    private static float closestAxisPointToMouseDistance;
    private static Vector2? closestAxisMousePos = null;
    private static Vector2? closestAxisMouseFromPos = null;
    private static Axis closestMouseAxis = Axis.X;
    private static Axis dragAxis = Axis.X;
    private static Vector2? dragStartToPos = null;
    private static Vector2? dragStartFromPos = null;
    private static float dragDistance = 0;
    private static Vector2 mousePos;
    private static Axis? lockedAxis = null;
    private static bool isUsing = false;

    public enum Axis
    {
        X,
        Y,
        Z,
    }

    public struct Style
    {
        public uint LockedAxisForegroundColor = 0xFFFFFFFF;
        public uint LockedAxisBackgroundColor = 0x10FFFFFF;

        public uint[] AxisForegroundColors = new uint[3]
        {
            0xFF3333FF,
            0xFF33FF33,
            0xFFFF3333,
        };

        public uint[] AxisBackgroundColors = new uint[3]
        {
            0x103333FF,
            0x1033FF33,
            0x10FF3333,
        };

        public Style()
        {
        }
    }

    public static bool IsUsing() => isUsing;

    public unsafe static bool DrawRotation(ref Matrix4x4 matrix, Vector2 size, bool worldSpace = false)
    {
        // decompose the matrix
        Vector3 scale;
        Quaternion rotation;
        Vector3 translation;
        Matrix4x4.Decompose(matrix, out scale, out rotation, out translation);

        Quaternion editingRotation = rotation;
        if(worldSpace)
            editingRotation = Quaternion.Identity;

        bool changed = DrawRotation(ref editingRotation, size);

        // recompose the matrix
        if(changed)
        {
            if (worldSpace)
            {
                matrix = Matrix4x4.CreateScale(scale);
                matrix = Matrix4x4.Transform(matrix, rotation);
                matrix = Matrix4x4.Transform(matrix, editingRotation);
                matrix.Translation = translation;
            }
            else
            {
                matrix = Matrix4x4.CreateScale(scale);
                matrix = Matrix4x4.Transform(matrix, editingRotation);
                matrix.Translation = translation;
            }
      
        }

        return changed;
    }

    public unsafe static bool DrawRotation(ref Quaternion rotation, Vector2 size)
    {
        bool changed = false;
        drawList = ImGui.GetWindowDrawList();

        BrioCamera* camera = (BrioCamera*)CameraManager.Instance()->GetActiveCamera();
        viewMatrix = camera->GetViewMatrix();

        // extract just rotation from camera view
        {
            Matrix4x4.Decompose(viewMatrix, out var cameraScale, out var cameraRotation, out var cameraTranslation);
            viewMatrix = Matrix4x4.CreateFromQuaternion(cameraRotation);
        }

        Matrix4x4 mat = Matrix4x4.CreateScale(-1, 1, 1);
        viewMatrix = viewMatrix * mat;

        float radius = Math.Min(size.X / 2, size.Y / 2) - 20;
        int lineThickness = 2;

        // reset context
        mousePos = ImGui.GetMousePos();
        closestAxisPointToMouseDistance = float.MaxValue;
        closestAxisMousePos = null;
        closestAxisMouseFromPos = null;
        isUsing = false;

   
        // create a transform matrix
        transformMatrix = Matrix4x4.CreateFromQuaternion(rotation);
        transformMatrix.Translation = new Vector3(0, -5, 0);

        if(ImGui.BeginChild("##imbriozmo", size))
        {
            Vector2 topPos = ImGui.GetWindowPos() + ImGui.GetWindowContentRegionMin();
            Vector2 botPos = ImGui.GetWindowPos() + ImGui.GetWindowContentRegionMax();
            bool isMouseOverArea = (mousePos.X > topPos.X && mousePos.Y > topPos.Y && mousePos.X < botPos.X && mousePos.Y < botPos.Y);

            if(isMouseOverArea)
                isUsing = true;

            Vector2 center = topPos + ((botPos - topPos) / 2);

            drawList.AddCircleFilled(center, radius, 0x50000000);

            DrawAxis(center, lineThickness, radius, Axis.X);
            DrawAxis(center, lineThickness, radius, Axis.Y);
            DrawAxis(center, lineThickness, radius, Axis.Z);


            // Mouse drag
            if(dragStartToPos != null && dragStartFromPos != null)
            {
                drawList.AddCircleFilled((Vector2)dragStartToPos, lineThickness * 2, style.AxisForegroundColors[(int)dragAxis]);

                Vector2 normal = Vector2.Normalize((Vector2)dragStartToPos - (Vector2)dragStartFromPos);

                if(!ImGui.IsMouseDown(ImGuiMouseButton.Left))
                {
                    dragStartToPos = null;
                    dragStartFromPos = null;
                    dragDistance = 0;
                    isUsing = false;
                }
                else
                {
                    isUsing = true;

                    Vector2 lhs = mousePos - (Vector2)dragStartToPos;
                    float newDragDistance = Vector2.Dot(lhs, normal);
                    float dragDelta = newDragDistance - dragDistance;
                    dragDistance = newDragDistance;

                    float angleChange = dragDelta / 200;

                    if(InputService.IsKeyBindDown(KeyBindEvents.Interface_IncrementSmallModifier))
                        angleChange /= 10;

                    if(InputService.IsKeyBindDown(KeyBindEvents.Interface_IncrementLargeModifier))
                        angleChange *= 10;

                    Quaternion rot = Quaternion.Identity;
                    if(dragAxis == Axis.X)
                    {
                        rot = Quaternion.CreateFromAxisAngle(Vector3.UnitX, angleChange);
                    }
                    if(dragAxis == Axis.Y)
                    {
                        rot = Quaternion.CreateFromAxisAngle(Vector3.UnitY, -angleChange);
                    }
                    if(dragAxis == Axis.Z)
                    {
                        rot = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, angleChange);
                    }

                    rotation = rotation * rot;
                    changed = true;
                }
            }

            // Mouse Hover
            else if(isMouseOverArea && closestAxisMousePos != null && (closestAxisPointToMouseDistance < axisHoverMouseDist || lockedAxis != null))
            {
                if(ImGui.IsMouseDown(ImGuiMouseButton.Left))
                {
                    dragStartToPos = closestAxisMousePos;
                    dragStartFromPos = closestAxisMouseFromPos;
                    dragAxis = closestMouseAxis;
                }
                if(ImGui.IsMouseClicked(ImGuiMouseButton.Right))
                {
                    if(lockedAxis == null)
                    {
                        lockedAxis = closestMouseAxis;
                    }
                    else
                    {
                        lockedAxis = null;
                    }
                }
                else
                {
                    float mouseWheel = ImGui.GetIO().MouseWheel / 100;

                    if(mouseWheel != 0)
                    {
                        if(InputService.IsKeyBindDown(KeyBindEvents.Interface_IncrementSmallModifier))
                            mouseWheel /= 10;

                        if(InputService.IsKeyBindDown(KeyBindEvents.Interface_IncrementLargeModifier))
                            mouseWheel *= 10;

                        Quaternion rot = Quaternion.Identity;
                        if(dragAxis == Axis.X)
                        {
                            rot = Quaternion.CreateFromAxisAngle(Vector3.UnitX, mouseWheel);
                        }
                        if(dragAxis == Axis.Y)
                        {
                            rot = Quaternion.CreateFromAxisAngle(Vector3.UnitY, -mouseWheel);
                        }
                        if(dragAxis == Axis.Z)
                        {
                            rot = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, mouseWheel);
                        }
                        changed = true;
                    }
                }

                drawList.AddCircle((Vector2)closestAxisMousePos, axisHoverMouseDist, style.AxisForegroundColors[(int)closestMouseAxis]);
            }

            ImGui.InvisibleButton("##imbriozmo_cover", size);
            ImGui.EndChild();
        }

        return changed;
    }

    private unsafe static void DrawAxis(
        Vector2 center,
        int thickness,
        float radius,
        Axis axis)
    {
        Vector3[] points3d = new Vector3[numPoints];
        for(int i = 0; i < points3d.Length; i++)
        {
            float p = (float)i / (float)(points3d.Length - 1);
            float r = p * (MathF.PI * 2);

            if(axis == Axis.Z)
            {
                points3d[i] = new Vector3(radius * MathF.Cos(r), radius * MathF.Sin(r), 0);
            }
            else if(axis == Axis.X)
            {
                points3d[i] = new Vector3(0, radius * MathF.Cos(r), radius * MathF.Sin(r));
            }
            else if(axis == Axis.Y)
            {
                points3d[i] = new Vector3(radius * MathF.Cos(r), 0, radius * MathF.Sin(r));
            }
        }

        float zClip = 0f;
        for(int i = 1; i < points3d.Length; i++)
        {
            Vector3 fromPoint = Vector3.Transform(points3d[i - 1], transformMatrix);
            fromPoint = Vector3.Transform(fromPoint, viewMatrix);

            Vector3 toPoint = Vector3.Transform(points3d[i], transformMatrix);
            toPoint = Vector3.Transform(toPoint, viewMatrix);

            bool isVisible = toPoint.Z < zClip;

            Vector2 fromPos = center + new Vector2(fromPoint.X, fromPoint.Y);
            Vector2 toPos = center + new Vector2(toPoint.X, toPoint.Y);

            uint segmentColor = isVisible ? style.AxisForegroundColors[(int)axis] : style.AxisBackgroundColors[(int)axis];

            if(lockedAxis == axis)
                segmentColor = isVisible ? style.LockedAxisForegroundColor : style.LockedAxisBackgroundColor;

            // check mouse
            if(isVisible && (lockedAxis == null || lockedAxis == axis))
            {
                float distToMouse = Vector2.Distance(toPos, mousePos);
                if(distToMouse <= closestAxisPointToMouseDistance)
                {
                    closestAxisPointToMouseDistance = distToMouse;
                    closestAxisMousePos = toPos;
                    closestAxisMouseFromPos = fromPos;
                    closestMouseAxis = axis;
                }
            }

            drawList.AddLine(fromPos, toPos, segmentColor, thickness);
        }
    }
}
