using System.Collections.Generic;
using System.Net.Sockets;

namespace FreeNet;

/// <summary>
/// This class creates a single large buffer which can be divided up and assigned to SocketAsyncEventArgs objects for
/// use with each socket I/O operation. This enables bufffers to be easily reused and gaurds against fragmenting heap
/// memory. The operations exposed on the BufferManager class are not thread safe.
/// </summary>
/// <param name="totalBytes">The total number of bytes controlled by the buffer pool</param>
internal class BufferManager(int totalBytes, int bufferSize)
{
    /// <summary>
    /// The stack of free indexes
    /// </summary>
    private readonly Stack<int> _freeIndexPool = new();

    /// <summary>
    /// The underlying byte array maintained by the Buffer Manager
    /// </summary>
    private byte[] _buffer;

    /// <summary>
    /// The current index into the buffer
    /// </summary>
    private int _currentIndex = 0;

    /// <summary>
    /// Removes the buffer from a SocketAsyncEventArg object. This frees the buffer back to the buffer pool
    /// </summary>
    public void FreeBuffer(SocketAsyncEventArgs args)
    {
        _freeIndexPool.Push(args.Offset);
        args.SetBuffer(null, 0, 0);
    }

    /// <summary>
    /// Allocates buffer space used by the buffer pool
    /// </summary>
    /// <remarks>
    /// Creates one big large buffer and divide that out to each SocketAsyncEventArg object
    /// </remarks>
    public void InitBuffer()
    {
        _buffer = new byte[totalBytes];
    }

    /// <summary>
    /// Assigns a buffer from the buffer pool to the specified SocketAsyncEventArgs object
    /// </summary>
    /// <returns>true if the buffer was successfully set, else false</returns>
    public bool SetBuffer(SocketAsyncEventArgs args)
    {
        if (_freeIndexPool.Count > 0)
        {
            args.SetBuffer(_buffer, _freeIndexPool.Pop(), bufferSize);
        }
        else
        {
            if ((totalBytes - bufferSize) < _currentIndex)
            {
                return false;
            }

            args.SetBuffer(_buffer, _currentIndex, bufferSize);
            _currentIndex += bufferSize;
        }

        return true;
    }
}