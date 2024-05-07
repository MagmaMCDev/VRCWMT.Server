using System.Collections;
using System.Collections.Immutable;

public class ThreadList<T> : ICollection<T>, IEnumerable<T>, IEnumerable, IList<T>, IReadOnlyCollection<T>, IReadOnlyList<T>, ICollection, IDisposable
{
    private readonly List<T> list = new();
    private readonly SemaphoreSlim semaphore = new(1, 1);
    private bool disposed = false;

    public T this[int index]
    {
        get
        {
            semaphore.Wait();
            try
            {
                return list[index];
            }
            finally
            {
                semaphore.Release();
            }
        }
        set
        {
            semaphore.Wait();
            try
            {
                list[index] = value;
            }
            finally
            {
                semaphore.Release();
            }
        }
    }

    public int Count
    {
        get
        {
            semaphore.Wait();
            try
            {
                return list.Count;
            }
            finally
            {
                semaphore.Release();
            }
        }
    }

    public bool IsReadOnly => false;

    public void Add(T item)
    {
        semaphore.Wait();
        try
        {
            list.Add(item);
        }
        finally
        {
            semaphore.Release();
        }
    }

    public void Clear()
    {
        semaphore.Wait();
        try
        {
            list.Clear();
        }
        finally
        {
            semaphore.Release();
        }
    }

    public bool Contains(T item)
    {
        semaphore.Wait();
        try
        {
            return list.Contains(item);
        }
        finally
        {
            semaphore.Release();
        }
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        semaphore.Wait();
        try
        {
            list.CopyTo(array, arrayIndex);
        }
        finally
        {
            semaphore.Release();
        }
    }

    public IEnumerator<T> GetEnumerator()
    {
        semaphore.Wait();
        try
        {
            return list.GetEnumerator();
        }
        finally
        {
            semaphore.Release();
        }
    }

    public int IndexOf(T item)
    {
        semaphore.Wait();
        try
        {
            return list.IndexOf(item);
        }
        finally
        {
            semaphore.Release();
        }
    }

    public void Insert(int index, T item)
    {
        semaphore.Wait();
        try
        {
            list.Insert(index, item);
        }
        finally
        {
            semaphore.Release();
        }
    }

    public bool Remove(T item)
    {
        semaphore.Wait();
        try
        {
            return list.Remove(item);
        }
        finally
        {
            semaphore.Release();
        }
    }

    public void RemoveAt(int index)
    {
        semaphore.Wait();
        try
        {
            list.RemoveAt(index);
        }
        finally
        {
            semaphore.Release();
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    void ICollection.CopyTo(Array array, int index)
    {
        semaphore.Wait();
        try
        {
            ((ICollection)list).CopyTo(array, index);
        }
        finally
        {
            semaphore.Release();
        }
    }

    bool ICollection.IsSynchronized => false;

    object ICollection.SyncRoot => ((ICollection)list).SyncRoot;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
                semaphore.Dispose();

            disposed = true;
        }
    }

    ~ThreadList()
    {
        Dispose(false);
    }

    public static implicit operator ThreadList<T>(T[] array)
    {
        var threadList = new ThreadList<T>();
        foreach (var item in array)
            threadList.Add(item);

        return threadList;
    }
    public static implicit operator ThreadList<T>(ImmutableArray<T> immutableArray)
    {
        var threadList = new ThreadList<T>();
        foreach (var item in immutableArray)
            threadList.Add(item);

        return threadList;
    }
    public static implicit operator ThreadList<T>(Span<T> span)
    {
        var threadList = new ThreadList<T>();
        foreach (var item in span)
            threadList.Add(item);

        return threadList;
    }
}