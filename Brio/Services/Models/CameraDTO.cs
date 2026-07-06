using Brio.Entities.Camera;
using Brio.Files;
using Brio.Game.Camera;
using MessagePack;
using System;

namespace Brio.Services.Models;

[Serializable]
[MessagePackObject(keyAsPropertyName: true)]
public class CameraDTO // TODO (Ken) names don't work with cameras. I don't have time to fix that atm
{
    [Key(0)] public CameraType CameraType { get; set; }

    [Key(1)] public VirtualCamera? Camera { get; set; }

    [Key(2)] public XATCameraFile? XATCamera { get; set; }

    [Key(3)] public string? ParentFolderId { get; set; }
}
