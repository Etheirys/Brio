using Brio.Entities;
using Brio.Entities.Camera;
using Brio.Game.Actor;
using Brio.Game.WorldObjects;
using Brio.Game.Camera;
using Brio.Game.World;
using Brio.Services;
using Brio.UI;
using Brio.UI.Controls.Core;
using Brio.UI.Controls.Stateless;
using Brio.UI.Entitites;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Numerics;
using Brio.Entities.Core;

namespace Brio.UI.Controls.Editors;

public class SpawnMenu
{
    private const float MenuWidth = 202f;

    private IServiceProvider provider = null!;

    private static VirtualCameraManager _cameraManager => Instance!.provider.GetRequiredService<VirtualCameraManager>();
    private static ActorSpawnService _actorSpawnService => Instance!.provider.GetRequiredService<ActorSpawnService>();
    private static LightingService _lightingService => Instance!.provider.GetRequiredService<LightingService>();
    private static WorldObjectService _worldObjectService => Instance!.provider.GetRequiredService<WorldObjectService>();
    private static EntityManager _entityManager => Instance!.provider.GetRequiredService<EntityManager>();
    private static ReferenceImageService _referenceImageService => Instance!.provider.GetRequiredService<ReferenceImageService>();

    private static string _pendingPath = "bgcommon/hou/outdoor/general/0332/asset/gar_b0_m0332.sgb"; // this has to be a valid path or we go booom

    static SpawnMenu Instance = null!;
    public void Initialize(IServiceProvider serviceProvider)
    {
        Instance = this;
        provider = serviceProvider;
    }

    public static bool NeedsActivation { get; private set; } = false;
    public static void OpenUnifiedSpawnMenu()
    {
        NeedsActivation = true;
    }

    public static void PrepareUnifiedSpawnMenu()
    {
        if(NeedsActivation)
        {
            NeedsActivation = false;

            var itemSpacing = ImGui.GetStyle().ItemSpacing.Y;
            var separatorHeight = itemSpacing * 2;

            float yOffset = ImGui.GetTextLineHeight() + separatorHeight + itemSpacing;

            var popupPos = ImGui.GetMousePos() - new Vector2(0, yOffset);
            ImGui.SetNextWindowPos(popupPos, ImGuiCond.Appearing);
        }
    }

    public static void DrawUnifiedSpawnMenu()
    {
        PrepareUnifiedSpawnMenu();

        using var popup = ImRaii.Popup("UnifiedSpawnMenuPopup", ImGuiWindowFlags.NoMove);
        if(popup.Success is false)
            return;

        var buttonSize = new Vector2(MenuWidth * ImGuiHelpers.GlobalScale, 0);

        using(ImRaii.PushColor(ImGuiCol.Button, UIConstants.Transparent))
        {
            if(_actorSpawnService != null)
            {
                ImBrio.SeparatorText("Actors");

                if(ImBrio.DrawIconButton(FontAwesomeIcon.User, "Actor", buttonSize))
                {
                    _actorSpawnService.CreateCharacter(out _, SpawnFlags.Default, true);
                }

                if(ImBrio.DrawIconButton(FontAwesomeIcon.PlusSquare, "Actor with Companion", buttonSize))
                {
                    _actorSpawnService.CreateCharacter(out _, SpawnFlags.WithCompanionSlot, false);
                }

                ImBrio.SeparatorText("Objects");

                if(ImBrio.DrawIconButton(FontAwesomeIcon.Cubes, "Prop", buttonSize))
                {
                    _worldObjectService.SpawnProp(new FFXIVClientStructs.FFXIV.Client.Graphics.Scene.WeaponCreateInfo
                    {
                        WeaponModelId =                                             
                        {
                            Id = 9001,
                            Type = 249,
                            Variant = 1,
                            Stain0 = 1,
                            Stain1 = 1,
                        },
                        AnimationVariant = 0,
                    });
                }
            
                if(ImBrio.DrawIconButton(FontAwesomeIcon.Couch, "Open Object Catalog", buttonSize))
                {
                    UIManager.Instance.ToggleFurnitureCatalogWindow();
                    ImGui.CloseCurrentPopup();
                }

                ImGui.SetNextItemWidth(buttonSize.X - ImGui.CalcTextSize("Path").X);
                ImGui.InputText("Path###spawn_furniture_path", ref _pendingPath, 512);

                if(ImBrio.DrawIconButton(FontAwesomeIcon.Chair, "Furniture Item", buttonSize))
                {
                    if(!string.IsNullOrWhiteSpace(_pendingPath))
                    {
                        _worldObjectService.SpawnFurniture(_pendingPath);
                        ImGui.CloseCurrentPopup();
                    }
                }

                if(ImBrio.DrawIconButton(FontAwesomeIcon.Boxes, "World Object", buttonSize))
                {
                    if(!string.IsNullOrWhiteSpace(_pendingPath))
                    {
                        _worldObjectService.SpawnBgObject(_pendingPath);
                        ImGui.CloseCurrentPopup();
                    }
                }

                if(ImBrio.DrawIconButton(FontAwesomeIcon.Burst, "VFX", buttonSize))
                {
                    if(!string.IsNullOrWhiteSpace(_pendingPath))
                    {
                        _worldObjectService.SpawnStaticVfx(_pendingPath);
                        ImGui.CloseCurrentPopup();
                    }
                }
            }

            if(_lightingService != null)
            {
                if(_actorSpawnService != null || _cameraManager != null)
                    ImGui.Spacing();

                ImBrio.SeparatorText("Lights");

                if(ImBrio.DrawIconButton(FontAwesomeIcon.Lightbulb, "Spot Light", buttonSize))
                {
                    _lightingService.SpawnLight(LightType.SpotLight);
                }

                if(ImBrio.DrawIconButton(FontAwesomeIcon.Lightbulb, "Point Light", buttonSize))
                {
                    _lightingService.SpawnLight(LightType.PointLight);
                }

                if(ImBrio.DrawIconButton(FontAwesomeIcon.Lightbulb, "Flat Light", buttonSize))
                {
                    _lightingService.SpawnLight(LightType.FlatLight);
                }
            }

            if(_cameraManager != null)
            {
                if(_actorSpawnService != null)
                    ImGui.Spacing();

                ImBrio.SeparatorText("Cameras");

                if(ImBrio.DrawIconButton(FontAwesomeIcon.Camera, "Brio Camera", buttonSize))
                {
                    _cameraManager.CreateCamera(CameraType.Game);
                    ImGui.CloseCurrentPopup();
                }

                if(ImBrio.DrawIconButton(FontAwesomeIcon.Video, "Free-Cam", buttonSize))
                {
                    _cameraManager.CreateCamera(CameraType.Free);
                    ImGui.CloseCurrentPopup();
                }
            }

            ImBrio.SeparatorText("Other");

            if(ImBrio.DrawIconButton(FontAwesomeIcon.Image, "Reference Image", buttonSize))
            {
                ImGui.CloseCurrentPopup();
                UIManager.Instance.FileDialogManager.OpenFileDialog(
                    "Select Reference Image###ref_img_picker",
                    "Images{.png,.jpg,.jpeg,.bmp}",
                    (success, path) =>
                    {
                        if(success)
                            _referenceImageService.Spawn(path);
                    });
            }

            if(ImBrio.DrawIconButton(FontAwesomeIcon.FolderPlus, "Folder", buttonSize))
            {
                _entityManager.CreateEntityOnEntityContainer<FolderEntity>($"Folder ({foldersCount++})");
            }
        }
    }

    static int foldersCount = 0;
}

