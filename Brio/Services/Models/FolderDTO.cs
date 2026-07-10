using MessagePack;
using System;

namespace Brio.Services.Models;

[Serializable]
[MessagePackObject(keyAsPropertyName: true)]
public class FolderDTO
{
    public string Id { get; set; } = string.Empty;
    public string? ParentId { get; set; }

    public string FriendlyName { get; set; } = "Folder";

    public bool AreChildrenHidden { get; set; }
}
