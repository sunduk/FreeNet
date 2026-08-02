namespace FreeNet;

/// <summary>
/// PacketBufferManager is a class that manages a pool of Packet objects for efficient reuse.
/// </summary>
/// <remarks>Not stable. Do not use this class!!</remarks>
public static class PacketBufferManager
{
    private static readonly Lock BufferLock = new();
    private static readonly Stack<Packet> Pool = new();
    private static int s_pool_capacity;

    public static void Initialize(int capacity)
    {
        Pool.Clear();
        s_pool_capacity = capacity;
        Allocate();
    }

    public static Packet Pop()
    {
        using (BufferLock.EnterScope())
        {
            if (Pool.Count <= 0)
            {
                Console.WriteLine("reallocate.");
                Allocate();
            }

            return Pool.Pop();
        }
    }

    public static void Push(Packet packet)
    {
        using (BufferLock.EnterScope())
        {
            Pool.Push(packet);
        }
    }

    private static void Allocate()
    {
        for (var i = 0; i < s_pool_capacity; ++i)
        {
            Pool.Push(new Packet());
        }
    }
}
