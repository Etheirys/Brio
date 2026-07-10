using MessagePack;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;

namespace Brio.Library.Tags;

[MessagePackObject(keyAsPropertyName: true)]
public class TagCollection : ICollection<Tag>, INotifyCollectionChanged
{
    public static readonly TagCollection Empty = [];

    private HashSet<Tag> _tags { get; set; } = [];

    private bool supressChangedEvents = false;

    public TagCollection()
    {
    }

    public TagCollection(TagCollection other) : this()
    {
        this.AddRange(other);
    }

    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    public int Count => this._tags.Count;

    public bool IsReadOnly => false;

    public Tag Add(string name, bool isToolGenerated = false)
    {
        Tag tag = Tag.Get(name, isToolGenerated);
        this.Add(tag);
        return tag;
    }

    public void Add(Tag? tag)
    {
        if(tag == null)
            return;

        this._tags.Add(tag);

        if(!this.supressChangedEvents)
        {
            this.CollectionChanged?.Invoke(this, new(NotifyCollectionChangedAction.Add, tag));
        }
    }

    public void Add(TagCollection? tags)
    {
        if(tags == null)
            return;

        foreach(Tag tag in tags)
        {
            this.Add(tag);
        }

        if(!this.supressChangedEvents)
        {
            this.CollectionChanged?.Invoke(this, new(NotifyCollectionChangedAction.Add, tags));
        }
    }

    public void Remove(Tag tag)
    {
        this._tags.Remove(tag);

        if(!this.supressChangedEvents)
        {
            this.CollectionChanged?.Invoke(this, new(NotifyCollectionChangedAction.Remove, tag));
        }
    }

    public void AddRange(IEnumerable<string> names)
    {
        this.supressChangedEvents = true;
        foreach(string name in names)
        {
            this.Add(name);
        }

        this.supressChangedEvents = false;
    }

    public void AddRange(IEnumerable<Tag> tags)
    {
        this.supressChangedEvents = true;
        foreach(Tag tag in tags)
        {
            this.Add(tag);
        }

        this.supressChangedEvents = false;
        this.CollectionChanged?.Invoke(this, new(NotifyCollectionChangedAction.Add, tags));
    }

    public void Replace(IEnumerable<Tag> tags)
    {
        this.supressChangedEvents = true;

        this.Clear();
        this.AddRange(tags);

        this.supressChangedEvents = false;
        this.CollectionChanged?.Invoke(this, new(NotifyCollectionChangedAction.Add, tags));
    }

    bool ICollection<Tag>.Remove(Tag tag)
    {
        bool removed = this._tags.Remove(tag);

        if(removed && !this.supressChangedEvents)
        {
            this.CollectionChanged?.Invoke(this, new(NotifyCollectionChangedAction.Remove, tag));
        }

        return removed;
    }

    public void Clear()
    {
        this._tags.Clear();

        if(!this.supressChangedEvents)
        {
            this.CollectionChanged?.Invoke(this, new(NotifyCollectionChangedAction.Reset));
        }
    }

    public bool Contains(Tag tag) => this._tags.Contains(tag);

    public bool Contains(TagCollection other)
    {
        foreach(Tag tag in other)
        {
            if(!this._tags.Contains(tag))
            {
                return false;
            }
        }

        return true;
    }

    public IEnumerator<Tag> GetEnumerator() => this._tags.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => this._tags.GetEnumerator();

    public override string ToString()
    {
        StringBuilder builder = new();
        foreach(Tag tag in this)
        {
            builder.Append(tag.Name);
            builder.Append(" ");
        }

        return builder.ToString();
    }

    public void CopyTo(Tag[] array, int arrayIndex)
    {
        this._tags.CopyTo(array, arrayIndex);
    }
}
