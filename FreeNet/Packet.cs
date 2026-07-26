using System;
using System.Text;

namespace FreeNet;

/// <summary>
/// byte[] 버퍼를 참조로 보관하여 pop_xxx 매소드 호출 순서대로 데이터 변환을 수행한다.
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
        // 참조로만 보관하여 작업한다. 복사가 필요하면 별도로 구현해야 한다.
        Buffer = buffer.Array;

        // 헤더는 읽을필요 없으니 그 이후부터 시작한다.
        Position = Defines.HEADERSIZE;
        Size = buffer.Count;

        // 프로토콜 아이디만 확인할 경우도 있으므로 미리 뽑아놓는다.
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
        // 참조로만 보관하여 작업한다. 복사가 필요하면 별도로 구현해야 한다.
        Buffer = buffer;

        // 헤더는 읽을필요 없으니 그 이후부터 시작한다.
        Position = Defines.HEADERSIZE;

        Owner = owner;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Packet"/> class.
    /// </summary>
    public Packet()
    {
        Buffer = new byte[1024];
    }

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

        // TODO: 다음 리팩토링 대상은 바로 여기다. CPacketBufferManager!!!
        //CPacket packet = CPacketBufferManager.pop();
        packet.SetProtocol(protocol_id);
        return packet;
    }

    /// <summary>
    /// Destroys the specified packet.
    /// </summary>
    /// <param name="packet">The packet.</param>
    public static void Destroy(Packet packet)
    {
        //CPacketBufferManager.push(packet);
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
        byte data = Buffer[Position];
        Position += sizeof(byte);
        return data;
    }

    /// <summary>
    /// Pops the float.
    /// </summary>
    /// <returns>System.Single.</returns>
    public float PopFloat()
    {
        float data = BitConverter.ToSingle(Buffer, Position);
        Position += sizeof(float);
        return data;
    }

    /// <summary>
    /// Pops the int16.
    /// </summary>
    /// <returns>System.Int16.</returns>
    public short PopInt16()
    {
        short data = BitConverter.ToInt16(Buffer, Position);
        Position += sizeof(short);
        return data;
    }

    /// <summary>
    /// Pops the int32.
    /// </summary>
    /// <returns>System.Int32.</returns>
    public int PopInt32()
    {
        int data = BitConverter.ToInt32(Buffer, Position);
        Position += sizeof(int);
        return data;
    }

    /// <summary>
    /// Pops the protocol identifier.
    /// </summary>
    /// <returns>System.Int16.</returns>
    public short PopProtocolId()
    {
        return PopInt16();
    }

    /// <summary>
    /// Pops the string.
    /// </summary>
    /// <returns>System.String.</returns>
    public string PopString()
    {
        // 문자열 길이는 최대 2바이트 까지. 0 ~ 32767
        short len = BitConverter.ToInt16(Buffer, Position);
        Position += sizeof(short);

        // 인코딩은 utf8로 통일한다.
        string data = Encoding.UTF8.GetString(Buffer, Position, len);
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
        byte[] temp_buffer = BitConverter.GetBytes(data);
        temp_buffer.CopyTo(Buffer, Position);
        Position += temp_buffer.Length;
    }

    /// <summary>
    /// Pushes the specified data.
    /// </summary>
    /// <param name="data">The data.</param>
    public void Push(int data)
    {
        byte[] temp_buffer = BitConverter.GetBytes(data);
        temp_buffer.CopyTo(Buffer, Position);
        Position += temp_buffer.Length;
    }

    /// <summary>
    /// Pushes the specified data.
    /// </summary>
    /// <param name="data">The data.</param>
    public void Push(string data)
    {
        byte[] temp_buffer = Encoding.UTF8.GetBytes(data);

        short len = (short)temp_buffer.Length;
        byte[] len_buffer = BitConverter.GetBytes(len);
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
        byte[] temp_buffer = BitConverter.GetBytes(data);
        temp_buffer.CopyTo(Buffer, Position);
        Position += temp_buffer.Length;
    }

    /// <summary>
    /// Pushes the int16.
    /// </summary>
    /// <param name="data">The data.</param>
    public void PushInt16(short data)
    {
        byte[] temp_buffer = BitConverter.GetBytes(data);
        temp_buffer.CopyTo(Buffer, Position);
        Position += temp_buffer.Length;
    }

    /// <summary>
    /// Records the size.
    /// </summary>
    public void RecordSize()
    {
        // header + body 를 합한 사이즈를 입력한다.
        byte[] header = BitConverter.GetBytes(Position);
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

        // 헤더는 나중에 넣을것이므로 데이터 부터 넣을 수 있도록 위치를 점프시켜놓는다.
        Position = Defines.HEADERSIZE;

        PushInt16(protocol_id);
    }
}