namespace FreeNet;

/// <summary>
/// Manages all currently connected users.
/// </summary>
public class ServerUserManager
{
    private readonly Lock _user;
    private readonly List<UserToken> _users;
    private long _heartbeatDuration;
    private Timer? _timerHeartbeat;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServerUserManager"/> class.
    /// </summary>
    public ServerUserManager()
    {
        _user = new Lock();
        _users = [];
    }

    /// <summary>
    /// Adds the specified user.
    /// </summary>
    /// <param name="user">The user.</param>
    public void Add(UserToken user)
    {
        using (_user.EnterScope())
        {
            _users.Add(user);
        }
    }

    /// <summary>
    /// Determines whether the specified user exists.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <returns><c>true</c> if the user exists; otherwise, <c>false</c>.</returns>
    public bool Exists(UserToken user)
    {
        using (_user.EnterScope())
        {
            return _users.Exists(obj => obj == user);
        }
    }

    /// <summary>
    /// Gets the total count.
    /// </summary>
    /// <returns>System.Int32.</returns>
    public int GetTotalCount()
    {
        using (_user.EnterScope())
        {
            return _users.Count;
        }
    }

    /// <summary>
    /// Removes the specified user.
    /// </summary>
    /// <param name="user">The user.</param>
    public void Remove(UserToken user)
    {
        using (_user.EnterScope())
        {
            _ = _users.Remove(user);
        }
    }

    /// <summary>
    /// Starts the heartbeat checking.
    /// </summary>
    /// <param name="checkIntervalSec">The check interval in seconds.</param>
    /// <param name="allowDurationSec">The allowed duration in seconds.</param>
    public void StartHeartbeatChecking(uint checkIntervalSec, uint allowDurationSec)
    {
        _heartbeatDuration = allowDurationSec * 10000000;
        _timerHeartbeat = new Timer(CheckHeartbeat, null, 1000 * checkIntervalSec, 1000 * checkIntervalSec);
    }

    /// <summary>
    /// Stops the heartbeat checking.
    /// </summary>
    public void StopHeartbeatChecking() => _timerHeartbeat?.Dispose();

    private void CheckHeartbeat(object? state)
    {
        var allowedTime = DateTime.Now.Ticks - _heartbeatDuration;

        using (_user.EnterScope())
        {
            for (var i = 0; i < _users.Count; ++i)
            {
                var heartbeatTime = _users[i].LatestHeartbeatTime;
                if (heartbeatTime >= allowedTime)
                {
                    continue;
                }

                _users[i].Disconnect();
            }
        }
    }
}
