#nullable disable

using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace Brio.Files;

public class ChangelogFile
{
    public string Tagline { get; set; }
    public string Subline { get; set; }

    [YamlMember(Alias = "changelog")]
    public List<ChangelogEntry> Changelog { get; set; }
}

public class ChangelogEntry
{
    public string Name { get; set; }
    public string Tagline { get; set; }
    public string Date { get; set; }
    public bool? IsCurrent { get; set; }
    public string Message { get; set; } 

    [YamlMember(DefaultValuesHandling = DefaultValuesHandling.Preserve)]
    public List<VersionEntry> Versions { get; set; }
}

public class VersionEntry
{
    public string Number { get; set; }
    public string Icon { get; set; }
    public List<string> Items { get; set; }
}
