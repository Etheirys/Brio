
//
// This code is from the Zero Electric Framework and provided to you under the MIT license 
//

//
// MIT License
// 
// Copyright (c) 2025 Ken M (minmoose), Zero Electric
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//  
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//

using System;
using System.Collections;
using System.Collections.Generic;

namespace Brio.Core;

public class ComponentSet<T> : IEnumerable<T>, IDisposable
{
    const int DefaultStartingSize = 50;
    const int DefaultResizeBy = 50;

    internal Queue<int> AvailableIndices = new(10);

    public int NextAvailableIndex { get; private set; } = 0;
    public int ActiveCount { get; private set; } = 0;

    public T[] Components;

    public ComponentSet()
    {
        Components = new T[DefaultStartingSize];
    }
    public ComponentSet(int StartingSize)
    {
        Components = new T[StartingSize];
    }

    public int Add(T component)
    {
        int indexAddress;

        if(AvailableIndices.Count > 0)
        {
            indexAddress = AvailableIndices.Dequeue();
        }
        else
        {
            indexAddress = NextAvailableIndex++;

            if(NextAvailableIndex == Components.Length)
                Resize();
        }

        Components[indexAddress] = component;
        ActiveCount++;

        return indexAddress;
    }

    public void ReplaceItem(int address, T item)
    {
        if(address < 0 || address >= NextAvailableIndex)
            Brio.Log.Fatal($"(ComponentSet::ReplaceItem) [{address}] Address is out of range.");

        Components[address] = item;
    }

    public void Remove(int address)
    {
        if(address < 0 || address >= NextAvailableIndex)
            Brio.Log.Fatal($"(ComponentSet::Remove) [{address}] Address is out of range.");

        Components[address] = default!;
        AvailableIndices.Enqueue(address);
        ActiveCount--;
    }

    public void Clear()
    {        
        Array.Clear(Components, 0, Components.Length);

        AvailableIndices.Clear();
        NextAvailableIndex = 0;
        ActiveCount = 0;
    }

    private void Resize(int size = DefaultResizeBy)
    {
        Array.Resize(ref Components, Components.Length + size);
    }

    public IEnumerator<T> GetEnumerator()
    {
        for(int i = 0; i < NextAvailableIndex; i++)
        {
            if(!EqualityComparer<T>.Default.Equals(Components[i], default!))
                yield return Components[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public void Dispose()
    {
        Clear();

        GC.SuppressFinalize(this);
    }
}
