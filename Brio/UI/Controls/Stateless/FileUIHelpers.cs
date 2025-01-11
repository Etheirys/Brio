using Brio.Capabilities.Actor;
using Brio.Capabilities.Posing;
using Brio.Config;
using Brio.Entities;
using Brio.Files;
using Brio.Game.Actor.Appearance;
using Brio.Game.Posing;
using Brio.Game.Scene;
using Brio.Game.Types;
using Brio.Library;
using Brio.Library.Filters;
using Brio.Resources;
using Brio.UI.Controls.Core;
using Brio.UI.Controls.Editors;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;

namespace Brio.UI.Controls.Stateless;

internal class FileUIHelpers
{
    public static void DrawProjectPopup(SceneService sceneService, EntityManager entityManager)
    {
        using var popup = ImRaii.Popup("DrawProjectPopup");
        if(popup.Success)
        {
            using(ImRaii.PushColor(ImGuiCol.Button, UIConstants.Transparent))
            {
                if(ImGui.Button("Save Project"))
                {

                }
                if(ImGui.Button("Load Project"))
                {

                }

                if(ImGui.Button("View Auto-Saves"))
                {

                }

                ImGui.Separator();

                if(ImGui.Button("Export Scene"))
                {
                    ShowExportSceneModal(entityManager);
                }
                if(ImGui.Button("Import Scene"))
                {
                    ShowImportSceneModal(sceneService);
                }
            }
        }
    }

    static bool freezeOnLoad = false;
    public static void DrawImportPoseMenuPopup(PosingCapability capability, bool showImportOptions = true)
    {
        using var popup = ImRaii.Popup("DrawImportPoseMenuPopup");

        if(popup.Success)
        {
            using(ImRaii.PushColor(ImGuiCol.Button, UIConstants.Transparent))
            {

                var size = ImGui.CalcTextSize("XXXX Freeze Actor on Import");
                size.Y = 44;

                ImGui.Checkbox("Freeze Actor on Import", ref freezeOnLoad);

                ImGui.Separator();

                if(ImGui.Button("Import as Body", size))
                {
                    ShowImportPoseModal(capability, asBody: true, freezeOnLoad: freezeOnLoad);
                }

                if(ImGui.Button("Import as Expression", size))
                {
                    ShowImportPoseModal(capability, asExpression: true, freezeOnLoad: freezeOnLoad);
                }

                ImGui.Separator();

                if(ImBrio.FontIconButton(FontAwesomeIcon.Cog))
                    ImGui.OpenPopup("import_optionsImportPoseMenuPopup");

                if(ImGui.IsItemHovered())
                    ImGui.SetTooltip("Import Options");

                ImGui.SameLine();

                if(ImGui.Button("Import with Options", new(size.X - 32, 25)))
                {
                    ShowImportPoseModal(capability, freezeOnLoad: freezeOnLoad);
                }
            }

            using(var popup2 = ImRaii.Popup("import_optionsImportPoseMenuPopup"))
            {
                if(popup2.Success && showImportOptions && Brio.TryGetService<PosingService>(out var service))
                {
                    PosingEditorCommon.DrawImportOptionEditor(service.DefaultImporterOptions);
                }
            }
        }
    }

    public static void ShowImportPoseModal(PosingCapability capability, PoseImporterOptions? options = null, bool asExpression = false, bool asBody = false, bool freezeOnLoad = false)
    {
        TypeFilter filter = new("Poses", typeof(CMToolPoseFile), typeof(PoseFile));


        if(ConfigurationService.Instance.Configuration.UseLibraryWhenImporting)
        {
            LibraryManager.Get(filter, (r) =>
            {
                if(r is CMToolPoseFile cmPose)
                {
                    capability.ImportPose(cmPose, options: options, asExpression: asExpression, asBody: asBody, freezeOnLoad: freezeOnLoad);
                }
                else if(r is PoseFile pose)
                {
                    capability.ImportPose(pose, options: options, asExpression: asExpression, asBody: asBody, freezeOnLoad: freezeOnLoad);
                }
            });
        }
        else
        {
            LibraryManager.GetWithFilePicker(filter, (r) =>
            {
                if(r is CMToolPoseFile cmPose)
                {
                    capability.ImportPose(cmPose, options: options, asExpression: asExpression, asBody: asBody, freezeOnLoad: freezeOnLoad);
                }
                else if(r is PoseFile pose)
                {
                    capability.ImportPose(pose, options: options, asExpression: asExpression, asBody: asBody, freezeOnLoad: freezeOnLoad);
                }
            });
        }
    }

    public static void ShowExportPoseModal(PosingCapability capability)
    {
        UIManager.Instance.FileDialogManager.SaveFileDialog("Export Pose###export_pose", "Pose File (*.pose){.pose}", "brio", ".pose",
                (success, path) =>
                {
                    if(success)
                    {
                        if(!path.EndsWith(".pose"))
                            path += ".pose";

                        var directory = Path.GetDirectoryName(path);
                        if(directory is not null)
                        {
                            ConfigurationService.Instance.Configuration.LastExportPath = directory;
                            ConfigurationService.Instance.Save();
                        }

                        capability.ExportSavePose(path);
                    }
                }, ConfigurationService.Instance.Configuration.LastExportPath, true);
    }

    public static void ShowImportCharacterModal(ActorAppearanceCapability capability, AppearanceImportOptions options)
    {
        List<Type> types = [typeof(ActorAppearanceUnion), typeof(AnamnesisCharaFile)];

        if(capability.CanMcdf)
            types.Add(typeof(MareCharacterDataFile));

        TypeFilter filter = new TypeFilter("Characters", [.. types]);

        if(ConfigurationService.Instance.Configuration.UseLibraryWhenImporting)
        {
            LibraryManager.Get(filter, (r) =>
            {
                if(r is ActorAppearanceUnion appearance)
                {
                    _ = capability.SetAppearance(appearance, options);
                }
                else if(r is AnamnesisCharaFile appearanceFile)
                {
                    _ = capability.SetAppearance(appearanceFile, options);
                }
                else if(r is MareCharacterDataFile mareFile)
                {
                    capability.LoadMcdf(mareFile.GetPath());
                }
            });

        }
        else
        {
            LibraryManager.GetWithFilePicker(filter, (r) =>
            {
                if(r is ActorAppearanceUnion appearance)
                {
                    _ = capability.SetAppearance(appearance, options);
                }
                else if(r is AnamnesisCharaFile appearanceFile)
                {
                    _ = capability.SetAppearance(appearanceFile, options);
                }
                else if(r is MareCharacterDataFile mareFile)
                {
                    capability.LoadMcdf(mareFile.GetPath());
                }
            });
        }
    }

    public static void ShowExportCharacterModal(ActorAppearanceCapability capability)
    {
        UIManager.Instance.FileDialogManager.SaveFileDialog("Export Character File###export_character_window", "Character File (*.chara){.chara}", "brio", "{.chara}",
                (success, path) =>
                {
                    if(success)
                    {
                        if(!path.EndsWith(".chara"))
                            path += ".chara";

                        var directory = Path.GetDirectoryName(path);
                        if(directory is not null)
                        {
                            ConfigurationService.Instance.Configuration.LastExportPath = directory;
                            ConfigurationService.Instance.Save();
                        }

                        capability.ExportAppearance(path);
                    }

                }, ConfigurationService.Instance.Configuration.LastExportPath, true);
    }

    public static void ShowImportMCDFModal(ActorAppearanceCapability capability)
    {
        UIManager.Instance.FileDialogManager.OpenFileDialog("Import MCDF File###import_character_window", "Mare Character Data File (*.mcdf){.mcdf}",
                 (success, paths) =>
                 {
                     if(success && paths.Count == 1)
                     {
                         var path = paths[0];
                         var directory = Path.GetDirectoryName(path);
                         if(directory is not null)
                         {
                             ConfigurationService.Instance.Configuration.LastMCDFPath = directory;
                             ConfigurationService.Instance.Save();
                         }
                         capability.LoadMcdf(path);
                     }
                 }, 1, ConfigurationService.Instance.Configuration.LastMCDFPath, true);
    }

    public static void ShowExportSceneModal(EntityManager entityManager)
    {
        UIManager.Instance.FileDialogManager.SaveFileDialog("Export Scene File###export_scene_window", "Brio Scene File (*.brioscn){.brioscn}", "brioscn", "{.brioscn}",
            (success, path) =>
            {
                if(success)
                {
                    Brio.Log.Info("Exporting scene...");
                    if(!path.EndsWith(".brioscn"))
                        path += ".brioscn";

                    var directory = Path.GetDirectoryName(path);
                    if(directory is not null)
                    {
                        ConfigurationService.Instance.Configuration.LastScenePath = directory;
                        ConfigurationService.Instance.Save();
                    }

                    SceneFile sceneFile = SceneService.GenerateSceneFile(entityManager);
                    ResourceProvider.Instance.SaveFileDocument(path, sceneFile);
                    Brio.Log.Info("Finished exporting scene");
                }
            }, ConfigurationService.Instance.Configuration.LastScenePath, true);
    }

    public static void ShowImportSceneModal(SceneService sceneService)
    {
        List<Type> types = [typeof(SceneFile)];
        TypeFilter filter = new("Scenes", [.. types]);

        LibraryManager.GetWithFilePicker(filter, r =>
        {
            Brio.Log.Verbose("Importing scene...");
            if(r is SceneFile importedFile)
            {
                sceneService.LoadScene(importedFile);
                Brio.Log.Verbose("Finished imported scene!");
            }
            else
            {
                throw new IOException("The file selected is not a valid scene file");
            }
        });
    }
}


