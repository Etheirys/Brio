using System;
using System.Collections.Generic;

namespace Brio.Config;
internal class LibraryConfiguration
{
    public float IconSize { get; set; } = 120;

    public Dictionary<string, FileSource> FileSources { get; set; } = new()
    {
        { "Brio Poses", new FileSource(Environment.SpecialFolder.MyDocuments, "/Brio/Poses/") },
        { "Brio Characters", new FileSource(Environment.SpecialFolder.MyDocuments, "/Brio/Characters/") },
        { "Anamnesis Poses", new FileSource(Environment.SpecialFolder.MyDocuments, "/Anamnesis/Poses/") },
        { "Anamnesis Characters", new FileSource(Environment.SpecialFolder.MyDocuments, "/Anamnesis/Characters/") },
    };

    public class FileSource
    {
        public string? Path { get; set; }
        public Environment.SpecialFolder? Root { get; set; }

        public FileSource()
        {
        }

        public FileSource(Environment.SpecialFolder? root, string? path)
        {
            Path = path;
            Root = root;
        }
    }
}
