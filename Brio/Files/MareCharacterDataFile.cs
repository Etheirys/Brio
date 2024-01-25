using Brio.Core;
using Brio.Library.Sources;
using Brio.Library.Tags;
using Brio.Resources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Brio.Files;


[FileType("Mare Character Data", "Images.FileIcon_Mcdf.png", ".mcdf", "Load")]
internal class MareCharacterDataFile : IFile
{
    public string? Description => null;
    public string? Author => null;
    public string? Version => null;
    public TagCollection? Tags => null;

    public void GetAutoTags(ref TagCollection tags)
    {
        tags.Add("Mare Synchronos");
    }

    public static MareCharacterDataFile? Load(string filePath)
    {
        // No support for actually loading an mcdf, as thats handled by IPC-ing to Mare.
        // But this class is used for the library tags, so lets just fake it with an empty file. =)
        return new MareCharacterDataFile();
    }
}
