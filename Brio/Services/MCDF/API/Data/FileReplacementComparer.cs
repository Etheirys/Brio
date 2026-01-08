using System;
using System.Collections.Generic;
using System.Linq;

namespace Brio.MCDF.API.Data;

public class FileReplacementComparer : IEqualityComparer<FileReplacement>
{
    private static readonly FileReplacementComparer _instance = new();

    private FileReplacementComparer()
    { }

    public static FileReplacementComparer Instance => _instance;

    public bool Equals(FileReplacement? x, FileReplacement? y)
    {
        if(x == null || y == null) return false;
        return x.ResolvedPath.Equals(y.ResolvedPath) && CompareLists(x.GamePaths, y.GamePaths);
    }

    public int GetHashCode(FileReplacement obj)
    {
        return HashCode.Combine(obj.ResolvedPath.GetHashCode(StringComparison.OrdinalIgnoreCase), GetOrderIndependentHashCode(obj.GamePaths));
    }

    private static bool CompareLists(HashSet<string> list1, HashSet<string> list2)
    {
        if(list1.Count != list2.Count)
            return false;

        for(int i = 0; i < list1.Count; i++)
        {
            if(!string.Equals(list1.ElementAt(i), list2.ElementAt(i), StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }

    private static int GetOrderIndependentHashCode<T>(IEnumerable<T> source) where T : notnull
    {
        int hash = 0;
        foreach(T element in source)
        {
            hash = unchecked(hash +
                EqualityComparer<T>.Default.GetHashCode(element));
        }
        return hash;
    }
}
