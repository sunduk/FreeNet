using System.Threading;

namespace FreeNet;

internal class HeartbeatSender
{
    private readonly uint _interval;
    private readonly UserToken _server;
    private readonly Timer _timerHeartbeat;
    private float _elapsedTime;

    public HeartbeatSender(UserToken server, uint interval)
    {
        _server = server;
        _interval = interval;
        _timerHeartbeat = new Timer(OnTimer, null, Timeout.Infinite, _interval * 1000);
    }

    public void Play()
    {
        _elapsedTime = 0;
        _ = _timerHeartbeat.Change(0, _interval * 1000);
    }

    public void Stop()
    {
        _elapsedTime = 0;
        _ = _timerHeartbeat.Change(Timeout.Infinite, Timeout.Infinite);
    }

    public void Update(float time)
    {
        _elapsedTime += time;
        if (_elapsedTime < _interval)
        {
            return;
        }

        _elapsedTime = 0.0f;
        Send();
    }

    private void OnTimer(object state) => Send();

    private void Send()
    {
        var msg = Packet.Create(UserToken.SYS_UPDATE_HEARTBEAT);
        _server.Send(msg);
    }
}