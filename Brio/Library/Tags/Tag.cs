using System;
using System.Collections.Generic;

namespace Brio.Library.Tags;
public class Tag : IEquatable<Tag?>
{
    private static readonly Dictionary<string, Tag> TagCache = new();

    private readonly string name;
    private readonly HashSet<string> aliases = new();

    private Tag(string name)
    {
        this.name = name;
    }

    public string Name => this.name;
    public IReadOnlyCollection<string> Aliases => this.aliases;

    // TODO: a lookup in resources for tag name
    public string DisplayName => this.name;

    public static implicit operator Tag(string name)
    {
        return Tag.Get(name);
    }

    public static bool operator ==(Tag? left, Tag? right)
    {
        return EqualityComparer<Tag>.Default.Equals(left, right);
    }

    public static bool operator !=(Tag? left, Tag? right)
    {
        return !(left == right);
    }

    public static Tag Get(string name)
    {
        lock(TagCache)
        {
            Tag? tag = null;
            if(TagCache.TryGetValue(name, out tag) && tag != null)
                return tag;

            tag = new(name);
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

        this.aliases.Add(alias);
        return this;
    }

    public virtual bool Search(string[]? query)
    {
        if(SearchUtility.Matches(this.Name, query))
            return true;

        if(SearchUtility.Matches(this.DisplayName, query))
            return true;

        foreach(string alias in this.aliases)
        {
            if(SearchUtility.Matches(alias, query))
            {
                return true;
            }
        }

        return false;
    }

    public override bool Equals(object? obj)
    {
        return this.Equals(obj as Tag);
    }

    public bool Equals(Tag? other)
    {
        return other is not null && this.Name == other.Name;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(this.Name);
    }
}
