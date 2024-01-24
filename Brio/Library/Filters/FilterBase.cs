namespace Brio.Library.Filters;

public abstract class FilterBase
{
    public readonly string Name;

    public FilterBase(string name)
    {
        Name = name;
    }

    public abstract void Clear();
    public abstract bool Filter(ILibraryEntry entry);
}
