using Brio.Capabilities.Actor;
using Brio.Capabilities.Posing;
using Brio.Config;
using Brio.Game.Actor.Appearance;
using Brio.Game.Posing;
using System.IO;

namespace Brio.UI.Controls.Stateless;

internal class FileUIHelpers
{
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
                        ConfigurationService.Instance.Configuration.Paths.LastPosePath = directory;

                        capability.ExportPose(path);
                    }
                }, ConfigurationService.Instance.Configuration.Paths.LastPosePath, true);
    }

    public static void ShowImportPoseModal(PosingCapability capability, PoseImporterOptions? options = null)
    {
        UIManager.Instance.FileDialogManager.OpenFileDialog("Import Pose###import_pose", "Pose File (*.pose | *.cmp){.pose,.cmp}",
                 (success, paths) =>
                 {
                     if(success && paths.Count == 1)
                     {
                         var path = paths[0];
                         var directory = Path.GetDirectoryName(path);
                         ConfigurationService.Instance.Configuration.Paths.LastPosePath = directory;

                         capability.ImportPose(path, options);
                     }
                 }, 1, ConfigurationService.Instance.Configuration.Paths.LastPosePath, true);
    }

    public static void ShowImportCharacterModal(ActorAppearanceCapability capability, AppearanceImportOptions options)
    {
        UIManager.Instance.FileDialogManager.OpenFileDialog("Import Character File###import_character_window", "Character File (*.chara){.chara}",
                 (success, paths) =>
                 {
                     if(success && paths.Count == 1)
                     {
                         var path = paths[0];
                         var directory = Path.GetDirectoryName(path);
                         ConfigurationService.Instance.Configuration.Paths.LastCharacterPath = directory;

                         capability.ImportAppearance(path, options);
                     }
                 }, 1, ConfigurationService.Instance.Configuration.Paths.LastCharacterPath, true);
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
                        ConfigurationService.Instance.Configuration.Paths.LastCharacterPath = directory;

                        capability.ExportAppearance(path);
                    }

                }, null, true);
    }

    public static void ShowImportMcdfModal(ActorAppearanceCapability capability)
    {
        UIManager.Instance.FileDialogManager.OpenFileDialog("Import MCDF File###import_character_window", "Mare Character Data File (*.mcdf){.mcdf}",
                 (success, paths) =>
                 {
                     if(success && paths.Count == 1)
                     {
                         var path = paths[0];
                         var directory = Path.GetDirectoryName(path);
                         ConfigurationService.Instance.Configuration.Paths.LastMcdfPath = directory;

                         capability.LoadMcdf(path);
                     }
                 }, 1, ConfigurationService.Instance.Configuration.Paths.LastMcdfPath, true);
    }
}
