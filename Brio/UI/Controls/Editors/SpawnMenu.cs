using Brio.Entities;
using Brio.Entities.Camera;
using Brio.Entities.Core;
using Brio.Game.Actor;
using Brio.Game.Camera;
using Brio.Game.Core;
using Brio.Game.World;
using Brio.Game.World.Interop;
using Brio.Game.WorldObjects;
using Brio.Services;
using Brio.UI.Controls.Core;
using Brio.UI.Controls.Stateless;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

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
    private static ObjectMonitorService _objectMonitorService => Instance!.provider.GetRequiredService<ObjectMonitorService>();

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

        ImBrio.BlurPopup();

        var buttonSize = new Vector2(MenuWidth * ImGuiHelpers.GlobalScale, 0);

        using(ImRaii.PushColor(ImGuiCol.Button, UIConstants.Transparent))
        {
            if(_actorSpawnService != null)
            {
                ImBrio.SeparatorText("Actors");

                if(ImBrio.IconButtonWithText(FontAwesomeIcon.User, "Actor", buttonSize))
                {
                    _actorSpawnService.CreateCharacter(out _, SpawnFlags.Default, true);
                }

                if(ImBrio.IconButtonWithText(FontAwesomeIcon.PlusSquare, "Actor with Companion", buttonSize))
                {
                    _actorSpawnService.CreateCharacter(out _, SpawnFlags.WithCompanionSlot, false);
                }

                if(ImBrio.IconButtonWithText(FontAwesomeIcon.Globe, "Actor from World...", buttonSize))
                {
                    ImGui.OpenPopup("FromWorldPopup");
                }

                using var fromWorldPopup = ImRaii.Popup("FromWorldPopup");
                if(fromWorldPopup.Success)
                {
                    var playerPosition = _objectMonitorService.ObjectTable.LocalPlayer?.Position ?? Vector3.Zero; // I hate this
                    var overworldActors = _objectMonitorService.GetOverworldActors().OrderBy(actor => Vector3.DistanceSquared(playerPosition, actor.Position));

                    if(!overworldActors.Any())
                    {
                        ImGui.TextDisabled("No world actors found");
                    }

                    foreach(var actor in overworldActors)
                    {
                        if(actor == null || !actor.IsValid())
                            return;

                        var distanceText = $" [{Vector3.Distance(playerPosition, actor.Position):0.0}]";

                        if(ImGui.MenuItem(string.IsNullOrWhiteSpace(actor?.Name.ToString()) ? $"Unknown {distanceText}##actor_{actor!.GameObjectId}" : $"{actor.Name} {distanceText}##actor_{actor.GameObjectId}"))
                        {
                            _actorSpawnService.AddFromWorld(actor!);
                            ImGui.CloseCurrentPopup();
                        }
                    }
                }
            }

            if(_worldObjectService != null)
            {
                ImGui.Spacing();
                ImBrio.SeparatorText("Objects");

                if(ImBrio.IconButtonWithText(FontAwesomeIcon.Couch, "Open Object Catalog", buttonSize))
                {
                    UIManager.Instance.ToggleCatalogWindow();
                    ImGui.CloseCurrentPopup();
                }

                if(ImBrio.IconButtonWithText(FontAwesomeIcon.Cubes, "Prop", buttonSize))
                {
                    _worldObjectService.SpawnProp(new FFXIVClientStructs.FFXIV.Client.Graphics.Scene.WeaponCreateInfo
                    {
                        // this is an apple
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

                if(ImBrio.IconButtonWithText(FontAwesomeIcon.Chair, "Furniture Item", buttonSize))
                {
                    _worldObjectService.SpawnFurniture("bgcommon/hou/outdoor/general/0332/asset/gar_b0_m0332.sgb");
                }

                if(ImBrio.IconButtonWithText(FontAwesomeIcon.Boxes, "World Object", buttonSize))
                {
                    _worldObjectService.SpawnBgObject("bg/ffxiv/fst_f1/twn/common/bgparts/f1t0_a0_taru1.mdl");
                }

                if(ImBrio.IconButtonWithText(FontAwesomeIcon.Burst, "VFX", buttonSize))
                {
                    _worldObjectService.SpawnStaticVfx("bgcommon/world/common/vfx_for_bg/eff/val_obj001_o.avfx");
                }
            }

            if(_lightingService != null)
            {
                ImGui.Spacing();
                ImBrio.SeparatorText("Lights");

                if(ImBrio.IconButtonWithText(FontAwesomeIcon.Lightbulb, "Spot Light", buttonSize))
                {
                    _lightingService.SpawnLight(LightType.SpotLight);
                }

                if(ImBrio.IconButtonWithText(FontAwesomeIcon.Lightbulb, "Point Light", buttonSize))
                {
                    _lightingService.SpawnLight(LightType.PointLight);
                }

                if(ImBrio.IconButtonWithText(FontAwesomeIcon.Lightbulb, "Flat Light", buttonSize))
                {
                    _lightingService.SpawnLight(LightType.FlatLight);
                }

                if(ImBrio.IconButtonWithText(FontAwesomeIcon.Globe, "Light from World...", buttonSize))
                {
                    ImGui.OpenPopup("FromWorldLightPopup");
                }

                using var fromWorldLightPopup = ImRaii.Popup("FromWorldLightPopup");
                if(fromWorldLightPopup.Success)
                {
                    var playerPosition = _objectMonitorService.ObjectTable.LocalPlayer?.Position ?? Vector3.Zero; // I hate this
                    var worldLights = new List<(nint, float)>();

                    unsafe
                    {
                        foreach(var light in _lightingService.WorldLights)
                        {
                            var blight = (BrioLight*)light;
                            worldLights.Add((light, Vector3.Distance(playerPosition, blight->Transform.Position)));
                        }

                        if(worldLights.Count == 0)
                        {
                            ImGui.TextDisabled("No world lights found");
                        }
                        else
                        {
                            if(ImGui.MenuItem($"Add All ({worldLights.Count})###containerwidgetpopup_addAllWorldLights"))
                            {
                                if(worldLights.Count == 0)
                                    return;

                                var folder = _entityManager.CreateEntityOnEntityContainer<FolderEntity>("World Lights");
                                folder.IsEditable = false;

                                foreach(var (light, _) in worldLights)
                                {
                                    var entity = _lightingService.AddWorldLight((BrioLight*)light);
                                    if(entity != null)
                                        _entityManager.AttachEntity(entity, folder, autoDetach: true);
                                }

                                ImGui.CloseCurrentPopup();
                            }
                            ImGui.Separator();
                            foreach(var (light, distance) in worldLights)
                            {
                                if(ImGui.MenuItem($"Light: {distance:F1}y##worldlight_{light}"))
                                {
                                    _lightingService.AddWorldLight((BrioLight*)light);
                                }
                            }
                        }
                    }
                }
            }

            if(_cameraManager != null)
            {
                ImGui.Spacing();
                ImBrio.SeparatorText("Cameras");

                if(ImBrio.IconButtonWithText(FontAwesomeIcon.Camera, "Brio Camera", buttonSize))
                {
                    _cameraManager.CreateCamera(CameraType.Game);
                    ImGui.CloseCurrentPopup();
                }

                if(ImBrio.IconButtonWithText(FontAwesomeIcon.Video, "Free-Cam", buttonSize))
                {
                    _cameraManager.CreateCamera(CameraType.Free);
                    ImGui.CloseCurrentPopup();
                }
            }

            ImGui.Spacing();
            ImBrio.SeparatorText("Other");

            if(ImBrio.IconButtonWithText(FontAwesomeIcon.Image, "Reference Image", buttonSize))
            {
                ImGui.CloseCurrentPopup();

                UIManager.Instance.FileDialogManager.OpenFileDialog(
                    "Select Reference Image###ref_img_picker",
                    "Images{.png,.jpg,.jpeg}",
                    (success, path) =>
                    {
                        if(success)
                            _referenceImageService.Spawn(path);
                    });
            }

            if(ImBrio.IconButtonWithText(FontAwesomeIcon.FolderPlus, "Folder", buttonSize))
            {
                _entityManager.CreateEntityOnEntityContainer<FolderEntity>($"Folder");
            }
        }
    }
}

