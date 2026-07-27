using CSampleServer;

using FreeNet;

NetworkService service = new(false);

// 콜백 매소드 설정.
service.SessionCreatedCallback += token =>
{
    GameUser user = new(token);
    lock (Users)
    {
        Users.Add(user);
    }
};

// 초기화.
service.Initialize(10000, 1024);
service.Listen("0.0.0.0", 7979, 100);

// 서버에서 하트비트 체크를 끌때 사용함.
// 스트레스 테스트를 하기 위해 FreeNet이 아닌 다른 클라이언트를 쓰는 경우등에 필요할것 같다.
// Remove below comments to disable heartbeat on server.
// (It maybe use to stress test from another client program not using FreeNet.)
//service.disable_heartbeat();

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

    public static void RemoveUser(GameUser user)
    {
        lock (Users)
        {
            _ = Users.Remove(user);
        }
    }
}
