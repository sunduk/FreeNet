using FreeNet;
using GameServer;

var service = new NetworkService(true);
// Set callback methods.
service.SessionCreatedCallback += OnSessionCreated;
// Initialize.
service.Initialize(10000, 1024);
service.Listen("0.0.0.0", 20000, 100);

Console.WriteLine("Started!");
while (true)
{
    var input = Console.ReadLine();
    Thread.Sleep(1000);
}

internal partial class Program
{
    private static readonly List<GameUser> Userlist = [];
    public static GameServerImpl GameMain { get; } = new();

    public static int GetConcurrentUserCount() => Userlist.Count;

    public static void OnSessionCreated(UserToken token)
    {
        var user = new GameUser(token);
        lock (Userlist)
        {
            Userlist.Add(user);
        }
    }

    public static void RemoveUser(GameUser user)
    {
        lock (Userlist)
        {
            _ = Userlist.Remove(user);

            GameMain.UserDisconnected(user);
        }
    }
}
