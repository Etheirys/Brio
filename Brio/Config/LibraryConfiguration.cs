using System;
using System.Collections.Generic;

namespace Brio.Config;
internal class LibraryConfiguration
{
    public float IconSize { get; set; } = 120;

    public List<FileSourceConfig> Files { get; set; } = new();
    public HashSet<string> Favorites { get; set; } = new();

    public void CheckDefaults()
    {
        if(Files.Count <= 0)
        {
            Files.Add(new FileSourceConfig("Brio Poses", Environment.SpecialFolder.MyDocuments, "/Brio/Poses/"));
            Files.Add(new FileSourceConfig("Brio Characters", Environment.SpecialFolder.MyDocuments, "/Brio/Characters/"));
            Files.Add(new FileSourceConfig("Anamnesis Poses", Environment.SpecialFolder.MyDocuments, "/Anamnesis/Poses/"));
            Files.Add(new FileSourceConfig("Anamnesis Characters", Environment.SpecialFolder.MyDocuments, "/Anamnesis/Characters/"));
        }
    }

    public void AddSource(SourceConfigBase config)
    {
        if (config is FileSourceConfig fileConfig)
        {
            this.Files.Insert(0, fileConfig);
        }
    }

    public void RemoveSource(SourceConfigBase config)
    {
        if(config is FileSourceConfig fileConfig)
        {
            this.Files.Remove(fileConfig);
        }
    }

    public List<SourceConfigBase> GetAllConfigs()
    {
        List<SourceConfigBase> results = new();
        results.AddRange(this.Files);
        return results;
    }

    public abstract class SourceConfigBase
    {
        public string Name { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;

        public SourceConfigBase(){}
        public SourceConfigBase(string name){ Name = name; }
    }

    public class FileSourceConfig : SourceConfigBase
    {
        public string? Path { get; set; }
        public Environment.SpecialFolder? Root { get; set; }

        public FileSourceConfig(){}
        public FileSourceConfig(string name, Environment.SpecialFolder? root, string? path)
           : base(name)
        {
            Path = path;
            Root = root;
        }
    }
}
