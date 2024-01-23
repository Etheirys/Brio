namespace Brio.Library;

public abstract class LibraryFilterBase
{
    public readonly string Name;

    public LibraryFilterBase(string name)
    {
        this.Name = name;
    }

    public abstract bool Filter(ILibraryEntry entry);
}
