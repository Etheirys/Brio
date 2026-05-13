using System;
using System.Collections.Generic;

namespace Brio.Library.Tags;

public class Tag : IEquatable<Tag?>
{
    private static readonly Dictionary<string, Tag> TagCache = [];

    private Tag(string name, bool isToolGenerated)
    {
        _name = name;
        IsToolGenerated = isToolGenerated;
    }
   
    // TODO: a lookup in resources for tag name (I don't know what this means - ken)
    private readonly string _name;
    public string DisplayName => _name;
    public string Name => _name;
   
    private readonly HashSet<string> _aliases = [];
    public IReadOnlyCollection<string> Aliases => _aliases;

    public bool IsToolGenerated { get; private set; } = false;

    public static Tag Get(string name, bool isToolGenerated = false)
    {
        lock(TagCache)
        {
            if(TagCache.TryGetValue(name, out Tag? tag) && tag != null)
                return tag;

            tag = new(name, isToolGenerated);
            TagCache.Add(name, tag);
            return tag;
        }
    }

    public static void ClearTagCache()
    {
        TagCache.Clear();
    }

    public static int TagCount()
    {
        return TagCache.Count;
    }

    public Tag WithAlias(string? alias)
    {
        if(alias == null)
            return this;

        this._aliases.Add(alias);
        return this;
    }

    public virtual bool Search(string[]? query)
    {
        if(SearchUtility.Matches(this.Name, query))
            return true;

        if(SearchUtility.Matches(this.DisplayName, query))
            return true;

        foreach(string alias in this._aliases)
        {
            if(SearchUtility.Matches(alias, query))
            {
                return true;
            }
        }

        return false;
    }

    public static implicit operator Tag(string name) => Get(name);
    public static bool operator ==(Tag? left, Tag? right) => EqualityComparer<Tag>.Default.Equals(left, right);
    public static bool operator !=(Tag? left, Tag? right) => !(left == right);

    public override bool Equals(object? obj) => Equals(obj as Tag);
    public bool Equals(Tag? other) => other is not null && Name == other.Name;

    public override int GetHashCode() => HashCode.Combine(Name);
}
