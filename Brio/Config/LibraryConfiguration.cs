using System.Collections.Generic;

namespace Brio.Config;
internal class LibraryConfiguration
{
    public float IconSize { get; set; } = 120;

    public Dictionary<string, string> Directories { get; set; } = new()
    {
        { "Brio Poses", "%MyDocuments%/Brio/Poses/" },
        { "Brio Characters", "%MyDocuments%/Brio/Characters/" },
        { "Anamnesis Poses", "%MyDocuments%/Anamnesis/Poses/" },
        { "Anamnesis Characters", "%MyDocuments%/Anamnesis/Characters/" },
    };
}
