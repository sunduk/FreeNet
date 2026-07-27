using System;

namespace FreeNet;

/// <summary>
/// Interface for message dispatching.
/// </summary>
public interface IMessageDispatcher
{
    /// <summary>
    /// Called when a message is received from the user.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <param name="buffer">The buffer.</param>
    void OnMessage(UserToken user, ArraySegment<byte> buffer);
}