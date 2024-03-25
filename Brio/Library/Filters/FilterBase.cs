namespace Brio.Library.Filters;

internal abstract class FilterBase
{
    public readonly string Name;

    public FilterBase(string name)
    {
        Name = name;
    }

    public abstract void Clear();
    public abstract bool Filter(EntryBase entry);
}
