using System;

namespace Brio.Services.Models;

[Flags]
public enum SceneImportOptions
{
    None = 0,
    Actors = 1 << 0,
    Cameras = 1 << 1,
    Lights = 1 << 2,
    WorldObjects = 1 << 3,
    Environment = 1 << 4,
    Folders = 1 << 5,

    All = Actors | Cameras | Lights | WorldObjects | Environment | Folders,
    Default = Actors | Cameras | Lights | WorldObjects | Folders,
}
