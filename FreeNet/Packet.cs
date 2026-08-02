using System;
using System.Text;

namespace FreeNet;

/// <summary>
/// Holds a byte[] buffer by reference and converts data in the order pop_xxx methods are called.
/// </summary>
public class Packet
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Packet"/> class.
    /// </summary>
    /// <param name="buffer">The buffer.</param>
    /// <param name="owner">The owner.</param>
    public Packet(ArraySegment<byte> buffer, UserToken owner)
    {
        // Operates on buffer references only. Implement copying separately if needed.
        Buffer = buffer.Array;

        // Skip the header and start after it.
        Position = Defines.HEADERSIZE;
        Size = buffer.Count;

        // Pre-read protocol ID because some paths only need to check it.
        ProtocolId = PopProtocolId();
        Position = Defines.HEADERSIZE;

        Owner = owner;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Packet"/> class.
    /// </summary>
    /// <param name="buffer">The buffer.</param>
    /// <param name="owner">The owner.</param>
    public Packet(byte[] buffer, UserToken owner)
    {
        // Operates on buffer references only. Implement copying separately if needed.
        Buffer = buffer;

        // Skip the header and start after it.
        Position = Defines.HEADERSIZE;

        Owner = owner;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Packet"/> class.
    /// </summary>
    public Packet() => Buffer = new byte[1024];

    /// <summary>
    /// Gets the buffer.
    /// </summary>
    /// <value>The buffer.</value>
    public byte[] Buffer { get; private set; }

    /// <summary>
    /// Gets the owner.
    /// </summary>
    /// <value>The owner.</value>
    public UserToken Owner { get; private set; }

    /// <summary>
    /// Gets the position.
    /// </summary>
    /// <value>The position.</value>
    public int Position { get; private set; }

    /// <summary>
    /// Gets the protocol identifier.
    /// </summary>
    /// <value>The protocol identifier.</value>
    public short ProtocolId { get; private set; }

    /// <summary>
    /// Gets the size.
    /// </summary>
    /// <value>The size.</value>
    public int Size { get; private set; }

    /// <summary>
    /// Creates the specified protocol identifier.
    /// </summary>
    /// <param name="protocol_id">The protocol identifier.</param>
    /// <returns>Packet.</returns>
    public static Packet Create(short protocol_id)
    {
        Packet packet = new();

        // TODO: Next refactoring target is this spot: PacketBufferManager!!!
        //Packet packet = PacketBufferManager.Pop();
        packet.SetProtocol(protocol_id);
        return packet;
    }

    /// <summary>
    /// Destroys the specified packet.
    /// </summary>
    /// <param name="packet">The packet.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
    public static void Destroy(Packet packet)
    {
        //PacketBufferManager.Push(packet);
    }

    /// <summary>
    /// Copies to the <paramref name="target"/>.
    /// </summary>
    /// <param name="target">The target.</param>
    public void CopyTo(Packet target)
    {
        target.SetProtocol(ProtocolId);
        target.Overwrite(Buffer, Position);
    }

    /// <summary>
    /// Overwrites the specified source.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="position">The position.</param>
    public void Overwrite(byte[] source, int position)
    {
        Array.Copy(source, Buffer, source.Length);
        Position = position;
    }

    /// <summary>
    /// Pops the byte.
    /// </summary>
    /// <returns>System.Byte.</returns>
    public byte PopByte()
    {
        var data = Buffer[Position];
        Position += sizeof(byte);
        return data;
    }

    /// <summary>
    /// Pops the float.
    /// </summary>
    /// <returns>System.Single.</returns>
    public float PopFloat()
    {
        var data = BitConverter.ToSingle(Buffer, Position);
        Position += sizeof(float);
        return data;
    }

    /// <summary>
    /// Pops the int16.
    /// </summary>
    /// <returns>System.Int16.</returns>
    public short PopInt16()
    {
        var data = BitConverter.ToInt16(Buffer, Position);
        Position += sizeof(short);
        return data;
    }

    /// <summary>
    /// Pops the int32.
    /// </summary>
    /// <returns>System.Int32.</returns>
    public int PopInt32()
    {
        var data = BitConverter.ToInt32(Buffer, Position);
        Position += sizeof(int);
        return data;
    }

    /// <summary>
    /// Pops the protocol identifier.
    /// </summary>
    /// <returns>System.Int16.</returns>
    public short PopProtocolId() => PopInt16();

    /// <summary>
    /// Pops the string.
    /// </summary>
    /// <returns>System.String.</returns>
    public string PopString()
    {
        // String length is stored in 2 bytes. Range: 0 ~ 32767.
        var len = BitConverter.ToInt16(Buffer, Position);
        Position += sizeof(short);

        // Standardize encoding as UTF-8.
        var data = Encoding.UTF8.GetString(Buffer, Position, len);
        Position += len;

        return data;
    }

    /// <summary>
    /// Pushes the specified data.
    /// </summary>
    /// <param name="data">The data.</param>
    public void Push(byte data)
    {
        Buffer[Position] = data;
        Position += sizeof(byte);
    }

    /// <summary>
    /// Pushes the specified data.
    /// </summary>
    /// <param name="data">The data.</param>
    public void Push(short data)
    {
        var temp_buffer = BitConverter.GetBytes(data);
        temp_buffer.CopyTo(Buffer, Position);
        Position += temp_buffer.Length;
    }

    /// <summary>
    /// Pushes the specified data.
    /// </summary>
    /// <param name="data">The data.</param>
    public void Push(int data)
    {
        var temp_buffer = BitConverter.GetBytes(data);
        temp_buffer.CopyTo(Buffer, Position);
        Position += temp_buffer.Length;
    }

    /// <summary>
    /// Pushes the specified data.
    /// </summary>
    /// <param name="data">The data.</param>
    public void Push(string data)
    {
        var temp_buffer = Encoding.UTF8.GetBytes(data);

        var len = (short)temp_buffer.Length;
        var len_buffer = BitConverter.GetBytes(len);
        len_buffer.CopyTo(Buffer, Position);
        Position += sizeof(short);

        temp_buffer.CopyTo(Buffer, Position);
        Position += temp_buffer.Length;
    }

    /// <summary>
    /// Pushes the specified data.
    /// </summary>
    /// <param name="data">The data.</param>
    public void Push(float data)
    {
        var temp_buffer = BitConverter.GetBytes(data);
        temp_buffer.CopyTo(Buffer, Position);
        Position += temp_buffer.Length;
    }

    /// <summary>
    /// Pushes the int16.
    /// </summary>
    /// <param name="data">The data.</param>
    public void PushInt16(short data)
    {
        var temp_buffer = BitConverter.GetBytes(data);
        temp_buffer.CopyTo(Buffer, Position);
        Position += temp_buffer.Length;
    }

    /// <summary>
    /// Records the size.
    /// </summary>
    public void RecordSize()
    {
        // Write the combined size of header + body.
        var header = BitConverter.GetBytes(Position);
        header.CopyTo(Buffer, 0);
    }

    /// <summary>
    /// Sets the protocol.
    /// </summary>
    /// <param name="protocol_id">The protocol identifier.</param>
    public void SetProtocol(short protocol_id)
    {
        ProtocolId = protocol_id;
        //this.buffer = new byte[1024];

        // Header is written later, so jump position to where data writing begins.
        Position = Defines.HEADERSIZE;

        PushInt16(protocol_id);
    }
}
