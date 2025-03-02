using System.Collections.Generic;
using System.Linq;

namespace Brio.Core;

public static class CollectionExtensions
{
    public static Stack<T> Trim<T>(this Stack<T> stack, int trimCount)
    {
        if(stack.Count <= trimCount)
            return stack;

        return new(stack.ToArray().Take(trimCount).Reverse());
    }
}
