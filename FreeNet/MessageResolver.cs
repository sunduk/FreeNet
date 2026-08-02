using System;

namespace FreeNet;

/// <summary>
/// Defines a callback delegate for completed message processing.
/// </summary>
/// <param name="buffer">The buffer.</param>
public delegate void CompletedMessageCallback(ArraySegment<byte> buffer);

/// <summary>
/// Defines class.
/// </summary>
internal class Defines
{
    /// <summary>
    /// The header size
    /// </summary>
    public static readonly short HEADERSIZE = 4;
}

/// <summary>
/// Parses data with a [header][body] structure.
/// - header: total message size, using the type size defined by Defines.HEADERSIZE (Int16 for 2 bytes, Int32 for 4 bytes).
/// - body: message payload.
/// If payload size never exceeds Int16.MaxValue, a 2-byte header is typically preferable.
/// </summary>
internal class MessageResolver
{
    // Buffer being assembled.
    private readonly byte[] _messageBuffer = new byte[1024];

    // Index into the in-progress buffer. Reset to 0 after one packet is completed.
    private int _currentPosition;

    // Message size.
    private int _messageSize;

    // Target position to read up to.
    private int _positionToRead;

    // Remaining bytes.
    private int _remainBytes;

    public MessageResolver()
    {
        _messageSize = 0;
        _currentPosition = 0;
        _positionToRead = 0;
        _remainBytes = 0;
    }

    public void ClearBuffer()
    {
        Array.Clear(_messageBuffer, 0, _messageBuffer.Length);

        _currentPosition = 0;
        _messageSize = 0;
    }

    /// <summary>
    /// Called whenever data is received from the socket buffer.
    /// Continues assembling packets and invokes the callback while data remains.
    /// If a full packet cannot be completed, keeps partial data in the buffer and waits for the next receive.
    /// </summary>
    /// <param name="buffer">Buffer containing received data.</param>
    /// <param name="offset">Start position for reading from the buffer.</param>
    /// <param name="transffered">Size of received data.</param>
    /// <param name="callback">Callback invoked when a packet is fully assembled.</param>
    public void OnReceive(byte[] buffer, int offset, int transffered, CompletedMessageCallback callback)
    {
        // Bytes to read from this receive.
        _remainBytes = transffered;

        // Position in the source buffer. Needed when multiple packets arrive together.
        var src_position = offset;

        // Continue while there is remaining data.
        while (_remainBytes > 0)
        {
            bool completed;

            // If the header is incomplete, read the header first.
            if (_currentPosition < Defines.HEADERSIZE)
            {
                // Set target position to the end of the header.
                _positionToRead = Defines.HEADERSIZE;

                completed = ReadUntil(buffer, ref src_position);
                if (!completed)
                {
                    // Not enough data yet; wait for the next receive.
                    return;
                }

                // Header is complete, so determine total message size.
                _messageSize = GetTotalMessageSize();

                // Treat non-positive message size as an invalid packet.
                if (_messageSize <= 0)
                {
                    ClearBuffer();
                    return;
                }

                // Next target position.
                _positionToRead = _messageSize;

                // If only the header was received, wait for the next receive for the body.
                if (_remainBytes <= 0)
                {
                    return;
                }
            }

            // Read the message body.
            completed = ReadUntil(buffer, ref src_position);

            if (completed)
            {
                // One packet has been fully assembled.
                var clone = new byte[_positionToRead];
                Array.Copy(_messageBuffer, clone, _positionToRead);
                ClearBuffer();
                callback(new ArraySegment<byte>(clone, 0, _positionToRead));
            }
        }
    }

    /// <summary>
    /// Gets total packet size (header + body). The header already stores total message size,
    /// so this only converts according to header width.
    /// </summary>
    /// <returns></returns>
    private int GetTotalMessageSize()
    {
        if (Defines.HEADERSIZE == 2)
        {
            return BitConverter.ToInt16(_messageBuffer, 0);
        }
        else if (Defines.HEADERSIZE == 4)
        {
            return BitConverter.ToInt32(_messageBuffer, 0);
        }

        return 0;
    }

    /// <summary>
    /// Copies bytes from the source buffer up to the configured target position.
    /// If data is insufficient, copies only the available remaining bytes.
    /// </summary>
    /// <param name="buffer">Buffer containing received data.</param>
    /// <param name="src_position">Start position for reading from the buffer.</param>
    /// <returns>True if target was reached; false if more data is needed.</returns>
    private bool ReadUntil(byte[] buffer, ref int src_position)
    {
        // Number of bytes to copy this time, accounting for previously copied bytes.
        var copy_size = _positionToRead - _currentPosition;

        // If fewer bytes remain, copy only what is available.
        if (_remainBytes < copy_size)
        {
            copy_size = _remainBytes;
        }

        // Copy into target buffer.
        Array.Copy(buffer, src_position, _messageBuffer, _currentPosition, copy_size);

        // Advance source buffer position.
        src_position += copy_size;

        // Advance target buffer position.
        _currentPosition += copy_size;

        // Update remaining byte count.
        _remainBytes -= copy_size;

        // Return false if target position has not been reached.
        return _currentPosition >= _positionToRead;
    }
}
