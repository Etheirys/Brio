using Brio.Capabilities.Posing;
using Brio.Config;
using Brio.Core;
using Brio.Entities;
using Brio.Entities.Actor;
using Brio.Files.Converters;
using Brio.Game.Actor.Appearance;
using Brio.Library.Sources;
using Brio.Resources;
using Brio.UI.Controls.Stateless;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures.TextureWraps;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using MessagePack;
using OneOf;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Brio.Files;

public class PoseFileInfo : AppliableActorFileInfoBase<PoseFile>
{
    private PosingCapability? _pendingCapability;
    private OneOf<PoseFile, CMToolPoseFile, PoseData>? _pendingPose;
    private bool _openPendingPosePopup;

    public override string Name => "Pose File";
    public override IDalamudTextureWrap Icon => ResourceProvider.Instance.GetResourceImage("Images.FileIcon_Pose.png");
    public override string Extension => ".pose";

    public PoseFileInfo(EntityManager entityManager, ConfigurationService configurationService) : base(entityManager, configurationService)
    {
    }

    public override void DrawActions(FileEntry fileEntry, bool isModal)
    {
        base.DrawActions(fileEntry, isModal);

        if(_openPendingPosePopup)
        {
            ImGui.OpenPopup("DrawImportPoseMenuPopup");
            _openPendingPosePopup = false;
        }

        FileUIHelpers.DrawImportPoseMenuPopup("libraryPose", _pendingCapability, importPose: _pendingPose);
    }

    protected override void OnApplyToActor(FileEntry fileEntry, ActorEntity actor)
    {
        if(Load(fileEntry.FilePath) is PoseFile file)
        {
            if(actor.TryGetCapability<PosingCapability>(out PosingCapability? capability) && capability != null)
            {
                if(_configService.Configuration.Library.UseFilenameAsActorName)
                {
                    actor.FriendlyName = fileEntry.Name;
                }

                _pendingCapability = capability;
                _pendingPose = file;
                _openPendingPosePopup = true;
            }
        }
    }

    protected override void Apply(PoseFile file, ActorEntity actor, bool asExpression)
    {
        if(actor.TryGetCapability<PosingCapability>(out PosingCapability? capability) && capability != null)
        {
            capability.ImportPose(file, asExpression: asExpression);
        }
    }
}

public record PoseMetaData(int ModelId, string? RaceSexId, int? FaceID);

[Serializable]
[MessagePackObject(keyAsPropertyName: true)]
public class PoseData : JsonDocumentBase
{
    public Bone ModelDifference { get; set; } = Transform.Identity;
    public Bone ModelAbsoluteValues { get; set; } = Transform.Identity;

    public Dictionary<string, Bone> Bones { get; set; } = [];
    public Dictionary<string, Bone> MainHand { get; set; } = [];
    public Dictionary<string, Bone> OffHand { get; set; } = [];
    public Dictionary<string, Bone> Prop { get; set; } = [];
    public Dictionary<string, Bone> Ornament { get; set; } = [];

    public Vector3 Position { get; set; }       // legacy & for better support for other pose tools
    public Quaternion Rotation { get; set; }    // legacy & for better support for other pose tools
    public Vector3 Scale { get; set; }          // legacy & for better support for other pose tools

    [MessagePackObject(keyAsPropertyName: true)]
    public class Bone
    {
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
        public Vector3 Scale { get; set; }

        public static implicit operator Transform(Bone bone)
        {
            return new Transform()
            {
                Position = bone.Position,
                Rotation = bone.Rotation,
                Scale = bone.Scale
            };
        }

        public static implicit operator Bone(Transform bone)
        {
            return new Bone()
            {
                Position = bone.Position,
                Rotation = bone.Rotation,
                Scale = bone.Scale
            };
        }
    }

    public void SanitizeBoneNames()
    {
        var newBones = new Dictionary<string, Bone>();
        foreach(var bone in Bones)
        {
            newBones[AnamnesisBoneNameConverter.AnamnesisToGame(bone.Key)] = bone.Value;
        }
        Bones = newBones;
    }
}

[Serializable]
[MessagePackObject(keyAsPropertyName: true)]
public class PoseFile : PoseData
{
    public string FileExtension => ".pose";

    public int FileVersion { get; set; } = 3;
    public string TypeName { get; set; } = "Brio Pose";

    public int ModelId { get; set; } = 0;
    public string? RaceSexId { get; set; } = null;
    public int? FaceID { get; set; } = null;

    // "2026.06.18.0000.0000" or above
    public unsafe string GameVersion { get; set; } = Framework.Instance()->GameVersionString;

    public (Tribes tribe, Genders gender, bool isNpc) GetRaceSexId()
    {
        if(RaceSexId == null)
            return (0, 0, false);

        return DataPathResolver.FromDataPath(short.Parse(RaceSexId));
    }
}
