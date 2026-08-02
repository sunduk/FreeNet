using System;
using System.Collections.Generic;
using System.Threading;

namespace FreeNet;

/// <summary>
/// Manages all currently connected users.
/// </summary>
public class ServerUserManager
{
    private readonly Lock _user;
    private readonly List<UserToken> _users;
    private long _heartbeatDuration;
    private Timer _timerHeartbeat;

    public ServerUserManager()
    {
        _user = new Lock();
        _users = [];
    }

    public void Add(UserToken user)
    {
        using (_user.EnterScope())
        {
            _users.Add(user);
        }
    }

    public bool Exists(UserToken user)
    {
        using (_user.EnterScope())
        {
            return _users.Exists(obj => obj == user);
        }
    }

    public int GetTotalCount()
    {
        using (_user.EnterScope())
        {
            return _users.Count;
        }
    }

    public void Remove(UserToken user)
    {
        using (_user.EnterScope())
        {
            _ = _users.Remove(user);
        }
    }

    public void StartHeartbeatChecking(uint check_interval_sec, uint allow_duration_sec)
    {
        _heartbeatDuration = allow_duration_sec * 10000000;
        _timerHeartbeat = new Timer(CheckHeartbeat, null, 1000 * check_interval_sec, 1000 * check_interval_sec);
    }

    public void StopHeartbeatChecking() => _timerHeartbeat.Dispose();

    private void CheckHeartbeat(object state)
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
