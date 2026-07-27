using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace FreeNet;

/// <summary>
/// Represents a collection of reusable SocketAsyncEventArgs objects. Initializes the object pool to the specified size.
/// The "capacity" parameter is the maximum number of SocketAsyncEventArgs objects the pool can hold
/// </summary>
/// <param name="capacity">The capacity.</param>
internal class SocketAsyncEventArgsPool(int capacity)
{
    private readonly Stack<SocketAsyncEventArgs> _pool = new(capacity);

    /// <summary>
    /// Gets the number of SocketAsyncEventArgs instances in the pool.
    /// </summary>
    /// <value>The number of SocketAsyncEventArgs instances in the pool.</value>
    public int Count => _pool.Count;

    /// <summary>
    /// Removes a SocketAsyncEventArgs instance from the pool and returns the object removed from the pool
    /// </summary>
    /// <returns>SocketAsyncEventArgs.</returns>
    public SocketAsyncEventArgs Pop()
    {
        lock (_pool)
        {
            return _pool.Pop();
        }
    }

    /// <summary>
    /// Add a SocketAsyncEventArg instance to the pool. The "item" parameter is the SocketAsyncEventArgs instance to add
    /// to the pool
    /// </summary>
    /// <param name="item">
    /// The <see cref="SocketAsyncEventArgs"/> instance containing the event data.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Items added to a SocketAsyncEventArgsPool cannot be null
    /// </exception>
    /// <exception cref="Exception">Already exist item.</exception>
    public void Push(SocketAsyncEventArgs item)
    {
        ArgumentNullException.ThrowIfNull(item);

        lock (_pool)
        {
            if (_pool.Contains(item))
            {
                throw new Exception("Already exist item.");
            }

            _pool.Push(item);
        }
    }
}