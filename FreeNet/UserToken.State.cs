namespace FreeNet;

public partial class UserToken
{
    /// <summary>
    /// Enum representing the current connection state.
    /// </summary>
    private enum State
    {
        /// <summary>Idle — not yet connected.</summary>
        Idle,

        /// <summary>Socket is connected and I/O loops are running.</summary>
        Connected,

        /// <summary>Socket is fully closed.</summary>
        Closed,
    }
}
