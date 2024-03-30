using Brio.Capabilities.Actor;
using Brio.Capabilities.Posing;
using Brio.Config;
using Brio.Files;
using Brio.Game.Actor.Appearance;
using Brio.Game.Posing;
using Brio.Game.Types;
using Brio.Library;
using Brio.Library.Filters;
using System;
using System.Collections.Generic;
using System.IO;

namespace Brio.UI.Controls.Stateless;

internal class FileUIHelpers
{
    public static void ShowImportPoseModal(PosingCapability capability, PoseImporterOptions? options = null)
    {
        TypeFilter filter = new TypeFilter("Poses", typeof(CMToolPoseFile), typeof(PoseFile));
        LibraryManager.Get(filter, (r) =>
        {
            if(r is CMToolPoseFile cmPose)
            {
                capability.ImportPose(cmPose, options);
            }
            else if(r is PoseFile pose)
            {
                capability.ImportPose(pose, options);
            }
        });
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

                        capability.ExportPose(path);
                    }
                }, null, true);
    }

    public static void ShowImportCharacterModal(ActorAppearanceCapability capability, AppearanceImportOptions options)
    {
        List<Type> types = [typeof(ActorAppearanceUnion), typeof(AnamnesisCharaFile)];

        if(capability.CanMcdf)
            types.Add(typeof(MareCharacterDataFile));

        TypeFilter filter = new TypeFilter("Characters", [.. types]);

        LibraryManager.Get(filter, async (r) =>
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
                await capability.LoadMcdfAsync(mareFile.GetPath());
            }
        });
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

                        capability.ExportAppearance(path);
                    }

                }, null, true);
    }

    public static void ShowImportMcdfModal(ActorAppearanceCapability capability)
    {
        UIManager.Instance.FileDialogManager.OpenFileDialog("Import MCDF File###import_character_window", "Mare Character Data File (*.mcdf){.mcdf}",
                 async (success, paths) =>
                 {
                     if(success && paths.Count == 1)
                     {
                         var path = paths[0];
                         var directory = Path.GetDirectoryName(path);
                         if(directory is not null)
                             ConfigurationService.Instance.Configuration.LastPath = directory;
                         await capability.LoadMcdfAsync(path);
                     }
                 }, 1, ConfigurationService.Instance.Configuration.LastPath, true);
    }
}
