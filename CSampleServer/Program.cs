using CSampleServer;

using FreeNet;
using Protocol;
using System.Collections.Concurrent;

NetworkService service = new(false);

// 콜백 매소드 설정.
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
                UserIds.TryAdd(newid, 0);
                break;
            }
        }
    }

    GameUser user = new GameUser(token, newid);

    lock (Users)
    {
        Users.Add(user);
    }
};

// 초기화.
service.Initialize(10000, 1024);
service.Listen("0.0.0.0", 3369, 100);

// 서버에서 하트비트 체크를 끌때 사용함.
// 스트레스 테스트를 하기 위해 FreeNet이 아닌 다른 클라이언트를 쓰는 경우등에 필요할것 같다.
// Remove below comments to disable heartbeat on server.
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
    private static readonly List<GameUser> Users = [];
    private static readonly ConcurrentDictionary<short, byte> UserIds = new();

    public static void RemoveUser(GameUser user)
    {
        lock (UserIds)
        {
            UserIds.Remove(user.Sig, out byte ret);
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
            if (UserIds.TryGetValue(user.Sig, out byte ret))
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
