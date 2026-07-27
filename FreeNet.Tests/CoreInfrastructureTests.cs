using System.Net.Sockets;

namespace FreeNet.Tests;

public class CoreInfrastructureTests
{
    [Test]
    public async Task Packet_round_trips_all_supported_payload_types()
    {
        var packet = Packet.Create(321);
        packet.Push((byte)3);
        packet.Push((short)-7);
        packet.Push(42);
        packet.Push(9.25f);
        packet.Push("world");
        packet.RecordSize();

        var parsed = new Packet(new ArraySegment<byte>(packet.Buffer, 0, packet.Position), null!);

        _ = await Assert.That(parsed.PopProtocolId()).IsEqualTo((short)321);
        _ = await Assert.That(parsed.PopByte()).IsEqualTo((byte)3);
        _ = await Assert.That(parsed.PopInt16()).IsEqualTo((short)-7);
        _ = await Assert.That(parsed.PopInt32()).IsEqualTo(42);
        _ = await Assert.That(parsed.PopFloat()).IsEqualTo(9.25f);
        _ = await Assert.That(parsed.PopString()).IsEqualTo("world");
    }

    [Test]
    public async Task Packet_copy_to_preserves_protocol_and_body()
    {
        var original = Packet.Create(77);
        original.Push(10);
        original.Push("copy");

        var copy = new Packet();
        original.CopyTo(copy);
        copy.RecordSize();

        var parsed = new Packet(new ArraySegment<byte>(copy.Buffer, 0, copy.Position), null!);

        _ = await Assert.That(parsed.ProtocolId).IsEqualTo((short)77);
        _ = await Assert.That(parsed.PopProtocolId()).IsEqualTo((short)77);
        _ = await Assert.That(parsed.PopInt32()).IsEqualTo(10);
        _ = await Assert.That(parsed.PopString()).IsEqualTo("copy");
    }

    [Test]
    public async Task MessageResolver_handles_fragmented_and_combined_packets()
    {
        var resolver = new MessageResolver();
        var completed = new List<ArraySegment<byte>>();

        var first = CreatePacketBytes(11, p => p.Push("alpha"));
        var second = CreatePacketBytes(12, p => p.Push(99));
        var combined = first.Concat(second).ToArray();

        resolver.OnReceive(combined, 0, 3, completed.Add);
        _ = await Assert.That(completed.Count).IsEqualTo(0);

        resolver.OnReceive(combined, 3, combined.Length - 3, completed.Add);
        _ = await Assert.That(completed.Count).IsEqualTo(2);

        var parsedFirst = new Packet(completed[0], null!);
        var parsedSecond = new Packet(completed[1], null!);

        _ = await Assert.That(parsedFirst.PopProtocolId()).IsEqualTo((short)11);
        _ = await Assert.That(parsedFirst.PopString()).IsEqualTo("alpha");
        _ = await Assert.That(parsedSecond.PopProtocolId()).IsEqualTo((short)12);
        _ = await Assert.That(parsedSecond.PopInt32()).IsEqualTo(99);
    }

    [Test]
    public async Task MessageResolver_discards_invalid_message_size_and_recovers()
    {
        var resolver = new MessageResolver();
        var completed = new List<ArraySegment<byte>>();

        var invalidHeaderOnly = BitConverter.GetBytes(0);
        resolver.OnReceive(invalidHeaderOnly, 0, invalidHeaderOnly.Length, completed.Add);

        var valid = CreatePacketBytes(99, p => p.Push((short)4));
        resolver.OnReceive(valid, 0, valid.Length, completed.Add);

        _ = await Assert.That(completed.Count).IsEqualTo(1);

        var parsed = new Packet(completed[0], null!);
        _ = await Assert.That(parsed.PopProtocolId()).IsEqualTo((short)99);
        _ = await Assert.That(parsed.PopInt16()).IsEqualTo((short)4);
    }

    [Test]
    public async Task DoubleBufferingQueue_swaps_input_to_output_on_getall()
    {
        ILogicQueue queue = new DoubleBufferingQueue();
        queue.Enqueue(Packet.Create(1));
        queue.Enqueue(Packet.Create(2));

        var firstBatch = queue.GetAll();
        _ = await Assert.That(firstBatch.Count).IsEqualTo(2);

        _ = firstBatch.Dequeue();
        _ = firstBatch.Dequeue();

        var secondBatch = queue.GetAll();
        _ = await Assert.That(secondBatch.Count).IsEqualTo(0);
    }

    [Test]
    public async Task SocketAsyncEventArgsPool_push_pop_and_count_work_as_expected()
    {
        var pool = new SocketAsyncEventArgsPool(2);
        var first = new SocketAsyncEventArgs();
        var second = new SocketAsyncEventArgs();

        pool.Push(first);
        pool.Push(second);
        _ = await Assert.That(pool.Count).IsEqualTo(2);

        var popped = pool.Pop();
        _ = await Assert.That(pool.Count).IsEqualTo(1);
        _ = await Assert.That(ReferenceEquals(popped, second)).IsTrue();
    }

    [Test]
    public async Task SocketAsyncEventArgsPool_rejects_duplicate_instances()
    {
        var pool = new SocketAsyncEventArgsPool(1);
        var item = new SocketAsyncEventArgs();
        pool.Push(item);

        var duplicateRejected = false;
        try
        {
            pool.Push(item);
        }
        catch (Exception)
        {
            duplicateRejected = true;
        }

        _ = await Assert.That(duplicateRejected).IsTrue();
    }

    [Test]
    public async Task SocketAsyncEventArgsPool_rejects_null_instance()
    {
        var pool = new SocketAsyncEventArgsPool(1);

        var nullRejected = false;
        try
        {
            pool.Push(null!);
        }
        catch (ArgumentNullException)
        {
            nullRejected = true;
        }

        _ = await Assert.That(nullRejected).IsTrue();
    }

    [Test]
    public async Task BufferManager_allocates_reuses_and_reports_exhaustion()
    {
        var manager = new BufferManager(totalBytes: 8, bufferSize: 4);
        manager.InitBuffer();

        var a = new SocketAsyncEventArgs();
        var b = new SocketAsyncEventArgs();
        var c = new SocketAsyncEventArgs();

        _ = await Assert.That(manager.SetBuffer(a)).IsTrue();
        _ = await Assert.That(manager.SetBuffer(b)).IsTrue();
        _ = await Assert.That(manager.SetBuffer(c)).IsFalse();

        var previousOffset = a.Offset;
        manager.FreeBuffer(a);

        _ = await Assert.That(manager.SetBuffer(c)).IsTrue();
        _ = await Assert.That(c.Offset).IsEqualTo(previousOffset);
    }

    [Test]
    public async Task PacketBufferManager_reuses_pushed_packet()
    {
        PacketBufferManager.Initialize(1);
        var first = PacketBufferManager.Pop();
        PacketBufferManager.Push(first);
        var second = PacketBufferManager.Pop();

        _ = await Assert.That(ReferenceEquals(first, second)).IsTrue();
    }

    [Test]
    public async Task PacketBufferManager_reallocates_when_pool_is_empty()
    {
        PacketBufferManager.Initialize(1);

        var first = PacketBufferManager.Pop();
        var second = PacketBufferManager.Pop();

        _ = await Assert.That(first).IsNotNull();
        _ = await Assert.That(second).IsNotNull();
        _ = await Assert.That(ReferenceEquals(first, second)).IsFalse();
    }

    private static byte[] CreatePacketBytes(short protocol, Action<Packet> writeBody)
    {
        var packet = Packet.Create(protocol);
        writeBody(packet);
        packet.RecordSize();

        var bytes = new byte[packet.Position];
        Array.Copy(packet.Buffer, bytes, packet.Position);
        return bytes;
    }
}
