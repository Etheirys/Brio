using Brio.Capabilities.ReferenceImage;
using Brio.Entities;
using Brio.UI.Controls.Stateless;
using Brio.UI.Theming;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Brio.Services;

public class ReferenceImageService(EntityManager entityManager)
{
    private readonly EntityManager _entityManager = entityManager;
    private readonly List<ReferenceImageEntity> _imageEntities = [];

    public ReferenceImageEntity Spawn(string filePath)
    {
        var entity = _entityManager.CreateEntityOnEntityContainer<ReferenceImageEntity>(filePath);
        _imageEntities.Add(entity);
        return entity;
    }

    public void Destroy(ReferenceImageEntity entity)
    {
        _imageEntities.Remove(entity);
        _entityManager.RemoveEntityFromEntityContainer(entity, dispose: true);
    }

    public void DrawWindows()
    {
        _imageEntities.RemoveAll(e => !e.IsAttached);

        foreach(var entity in _imageEntities)
        {
            if(!entity.IsWindowOpen) continue;

            DrawEntityWindow(entity);
        }
    }

    private static void DrawEntityWindow(ReferenceImageEntity entity)
    {
        var windowVisible = false;
        var showChrome = entity.WasHovered;
        var isOpen = entity.IsWindowOpen;

        var flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoCollapse;

        if(entity.IsLocked)
            flags |= ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove;

        ImGui.SetNextWindowBgAlpha(showChrome ? 0.7f : 0.0f);
        ImGui.SetNextWindowSizeConstraints(new Vector2(100, 100), new Vector2(float.MaxValue, float.MaxValue));
        ImGui.SetNextWindowSize(new Vector2(800, 600), ImGuiCond.Appearing);

        using(ImRaii.PushStyle(ImGuiStyleVar.WindowBorderSize, 0f))
            windowVisible = ImGui.Begin($"###ref_img_{entity.Id}", ref isOpen, flags);

        if(windowVisible is false)
        {
            entity.IsWindowOpen = isOpen;
            entity.WasHovered = false;

            ImGui.End();

            return;
        }

        entity.IsWindowOpen = isOpen;

        bool isHovered = ImGui.IsWindowHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);

        if(isHovered && ImGui.GetIO().MouseWheel != 0)
            entity.Zoom = Math.Clamp(entity.Zoom + ImGui.GetIO().MouseWheel * 0.1f, 0.1f, 5.0f);

        if(!entity.IsLocked)
        {
            if(isHovered && !ImGui.IsAnyItemActive() && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
                ImGui.SetWindowPos(ImGui.GetWindowPos() + ImGui.GetIO().MouseDelta);

            if(isHovered && ImGui.IsMouseDragging(ImGuiMouseButton.Right))
                entity.PanOffset += ImGui.GetIO().MouseDelta;
        }

        entity.TryGetCapability<ReferenceImageCapability>(out var cap);
        bool hasImage = cap?.Texture is not null;

        var contentStart = ImGui.GetCursorPos();

        if(hasImage)
            DrawZoomedImage(cap!.Texture!, entity.Zoom, entity.Opacity, entity.PanOffset);
        else
            ImGui.Dummy(ImGui.GetContentRegionAvail());

        if(showChrome)
        {
            ImGui.SetCursorPos(contentStart);
            if(hasImage)
            {
                DrawOverlayToolbar(entity, cap!);
            }
            else
            {
                ImGui.TextDisabled("Image loading...");
            }
        }

        entity.WasHovered = isHovered;

        ImGui.End();
    }

    private static void DrawOverlayToolbar(ReferenceImageEntity entity, ReferenceImageCapability cap)
    {
        var bgHeight = ImGui.GetFrameHeightWithSpacing() + ImGui.GetStyle().ItemSpacing.Y;
        var bgMin = ImGui.GetCursorScreenPos();
        var bgMax = bgMin + new Vector2(ImGui.GetContentRegionAvail().X, bgHeight);

        ImGui.GetWindowDrawList().AddRectFilled(bgMin, bgMax, 0xAA000000, 10);

        ImGui.SetCursorPos(new Vector2(ImGui.GetCursorPosX() + 10, ImGui.GetCursorPosY() + ImGui.GetStyle().ItemSpacing.Y));

        if(ImBrio.FontIconButton($"###{entity.Id}_delete", FontAwesomeIcon.Trash, "Delete Image", bordered: false))
            cap.Destroy();

        ImGui.SameLine();

        string lockTip = entity.IsLocked ? "Unlock" : "Lock Position";
        using(ImRaii.PushColor(ImGuiCol.Button, ThemeManager.CurrentTheme.Accent.AccentColor, entity.IsLocked))
            if(ImBrio.FontIconButton($"###{entity.Id}_lock", entity.IsLocked ? FontAwesomeIcon.Lock : FontAwesomeIcon.Unlock, lockTip, bordered: false))
                entity.IsLocked = !entity.IsLocked;

        ImGui.SameLine();

        float reservedWidth = ((90f * ImGuiHelpers.GlobalScale) + ImGui.GetStyle().ItemSpacing.X) * 2;
        string displayName = TruncateToWidth(entity.FriendlyName, Math.Max(ImGui.GetContentRegionAvail().X - reservedWidth, 0));
        ImGui.Text(displayName);
        if(displayName != entity.FriendlyName && ImGui.IsItemHovered())
            ImGui.SetTooltip(entity.FriendlyName);

        ImGui.SameLine();

        ImGui.SetNextItemWidth(90 * ImGuiHelpers.GlobalScale);
        float opacityPercent = entity.Opacity * 100f;
        if(ImGui.SliderFloat($"###opacity_{entity.Id}", ref opacityPercent, 10f, 100f, "%.0f%%"))
            entity.Opacity = opacityPercent / 100f;
        ImBrio.AttachToolTip("Opacity");

        ImGui.SameLine();

        ImGui.SetNextItemWidth(90 * ImGuiHelpers.GlobalScale);
        float zoom = entity.Zoom;
        if(ImGui.SliderFloat($"###zoom_{entity.Id}", ref zoom, 0.1f, 3.0f, "%.1fx"))
            entity.Zoom = zoom;
        ImBrio.AttachToolTip("Zoom (Scroll Wheel)");
    }

    private static void DrawZoomedImage(IDalamudTextureWrap texture, float zoom, float opacity, Vector2 panOffset)
    {
        var available = ImGui.GetContentRegionAvail();

        if(texture.Width <= 0 || texture.Height <= 0 || available.X <= 0 || available.Y <= 0)
            return;

        float widthScale = available.X / texture.Width;
        float heightScale = available.Y / texture.Height;
        float scale = Math.Min(widthScale, heightScale) * zoom;

        float width = texture.Width * scale;
        float height = texture.Height * scale;

        float offsetX = ((available.X - width) * 0.5f) + panOffset.X;
        float offsetY = ((available.Y - height) * 0.5f) + panOffset.Y;

        var baseScreenPos = ImGui.GetCursorScreenPos();
        var imageMin = baseScreenPos + new Vector2(offsetX, offsetY);
        var imageMax = imageMin + new Vector2(width, height);

        uint alpha = (uint)(Math.Clamp(opacity, 0f, 1f) * 255) & 0xFF;
        uint tintColor = (alpha << 24) | (255u << 16) | (255u << 8) | 255u;

        ImGui.GetWindowDrawList().AddImage(texture.Handle, imageMin, imageMax, Vector2.Zero, Vector2.One, tintColor);

        ImGui.Dummy(available);
    }

    private static string TruncateToWidth(string text, float maxWidth)
    {
        if(maxWidth <= 0 || ImGui.CalcTextSize(text).X <= maxWidth)
            return text;

        const string ellipsis = "...";
        float ellipsisWidth = ImGui.CalcTextSize(ellipsis).X;

        for(int i = text.Length - 1; i > 0; i--)
        {
            if(ImGui.CalcTextSize(text[..i]).X + ellipsisWidth <= maxWidth)
                return text[..i] + ellipsis;
        }

        return ellipsis;
    }
}
