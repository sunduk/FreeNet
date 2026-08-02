using CSampleServer;

using FreeNet;
using Protocol;
using System.Collections.Concurrent;

NetworkService service = new(false);

// Set callback methods.
service.SessionCreatedCallback += token =>
{
    short newid = 0;
    lock (UserIds)
    {
        for (short i = 1; i <= short.MaxValue; i++)
        {
            if (!UserIds.ContainsKey(i))
            {
                newid = i;
                _ = UserIds.TryAdd(newid, 0);
                break;
            }
        }
    }

    var user = new GameUser(token, newid);

    lock (Users)
    {
        Users.Add(user);
    }
};

// Initialize.
service.Initialize(10000, 1024);
service.Listen("0.0.0.0", 3369, 100);

// Use this to disable heartbeat checks on the server. It is useful for stress tests with clients
// that do not use FreeNet. Remove the comments below to disable heartbeat on the server.
// (It maybe use to stress test from another client program not using FreeNet.)
service.DisableHeartbeat();

Console.WriteLine("Started!");
while (true)
{
    //Console.Write(".");
    var input = Console.ReadLine();
    if (input.Equals("users"))
    {
        Console.WriteLine(service.Usermanager.GetTotalCount());
    }

    Thread.Sleep(1000);
}

internal partial class Program
{
    private static readonly ConcurrentDictionary<short, byte> UserIds = new();
    private static readonly List<GameUser> Users = [];

    public static void RemoveUser(GameUser user)
    {
        lock (UserIds)
        {
            _ = UserIds.Remove(user.Sig, out var ret);
        }

        lock (Users)
        {
            _ = Users.Remove(user);
        }
    }

    public static void SendAll(Packet pkt)
    {
        foreach (var user in Users)
        {
            if (UserIds.TryGetValue(user.Sig, out var ret))
            {
                if (ret == 0)
                {
                    var msg = new SCUserInfo
                    {
                        UserID = user.Sig
                    };
                    //CPacket msg = CPacket.create((short)GameServer.PROTOCOL.USER_INFO);
                    //msg.push(user.sig);
                    //user.send(msg);

                    user.Send(msg.ToPacket());
                    if (!UserIds.TryUpdate(user.Sig, 1, 0))
                    {
                        Console.WriteLine("id info send state update fail!");
                    }
                }
            }
        }

        foreach (var user in Users)
        {
            user.Send(pkt);
        }
    }
}
