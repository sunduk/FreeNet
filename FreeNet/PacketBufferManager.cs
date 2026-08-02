using System;
using System.Collections.Generic;
using System.Threading;

namespace FreeNet;

/// <summary>
/// PacketBufferManager is a class that manages a pool of Packet objects for efficient reuse.
/// </summary>
/// <remarks>Not stable. Do not use this class!!</remarks>
public class PacketBufferManager
{
    private static readonly Lock BufferLock = new();
    private static Stack<Packet> s_pool;
    private static int s_pool_capacity;

    public static void Initialize(int capacity)
    {
        s_pool = new Stack<Packet>();
        s_pool_capacity = capacity;
        Allocate();
    }

    public static Packet Pop()
    {
        using (BufferLock.EnterScope())
        {
            if (s_pool.Count <= 0)
            {
                Console.WriteLine("reallocate.");
                Allocate();
            }

            return s_pool.Pop();
        }
    }

    public static void Push(Packet packet)
    {
        using (BufferLock.EnterScope())
        {
            s_pool.Push(packet);
        }
    }

    private static void Allocate()
    {
        for (var i = 0; i < s_pool_capacity; ++i)
        {
            s_pool.Push(new Packet());
        }
    }
}
