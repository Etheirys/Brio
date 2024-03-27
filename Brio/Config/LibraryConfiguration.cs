using System;
using System.Collections.Generic;

namespace Brio.Config;
internal class LibraryConfiguration
{
    public float IconSize { get; set; } = 120;

    public List<FileSourceConfig> Files { get; set; } = [];
    public HashSet<string> Favorites { get; set; } = [];

    public Dictionary<string, string> LastBrowsePaths { get; set; } = [];

    public bool ReturnLibraryToLastLocation { get; set; } = true;

    public void ReEstablishDefaultPaths()
    {
        if(Files.Count <= 0)
        {
            Files.Add(new FileSourceConfig() { Name = "Brio Poses", Path = "/Brio/Poses/", Root = Environment.SpecialFolder.MyDocuments, CanEdit = false });
            Files.Add(new FileSourceConfig() { Name = "Brio Characters", Path = "/Brio/Characters/", Root = Environment.SpecialFolder.MyDocuments, CanEdit = false });
            Files.Add(new FileSourceConfig() { Name = "Anamnesis Poses", Path = "/Anamnesis/Poses/", Root = Environment.SpecialFolder.MyDocuments, CanEdit = false });
            Files.Add(new FileSourceConfig() { Name = "Anamnesis Characters", Path = "/Anamnesis/Characters/", Root = Environment.SpecialFolder.MyDocuments, CanEdit = false });
        }
    }

    public void AddSource(SourceConfigBase config)
    {
        if(config is FileSourceConfig fileConfig)
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

    public List<SourceConfigBase> GetAll() => [.. this.Files];

    public abstract class SourceConfigBase
    {
        public string Name { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;
        public bool CanEdit { get; set; } = true;
    }

    public class FileSourceConfig : SourceConfigBase
    {
        public string? Path { get; set; } = string.Empty;
        public Environment.SpecialFolder? Root { get; set; } = Environment.SpecialFolder.MyComputer;
    }
}
