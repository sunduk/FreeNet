using CSampleClient;
using FreeNet;
using System.Net;

PacketBufferManager.Initialize(2000);

// CNetworkService객체는 메시지의 비동기 송,수신 처리를 수행한다. 메시지 송,수신은 서버, 클라이언트 모두 동일한 로직으로 처리될 수 있으므로
// CNetworkService객체를 생성하여 Connector객체에 넘겨준다.
NetworkService service = new(true);

// endpoint정보를 갖고있는 Connector생성. 만들어둔 NetworkService객체를 넣어준다.
Connector connector = new(service);

List<IPeer> gameServers = [];

// 접속 성공시 호출될 콜백 매소드 지정.
connector.ConnectedCallback += serverToken =>
{
    lock (gameServers)
    {
        IPeer server = new RemoteServerPeer(serverToken);
        serverToken.OnConnected();
        gameServers.Add(server);
        Console.WriteLine("Connected!");
    }
};

IPEndPoint endpoint = new(IPAddress.Parse("127.0.0.1"), 7979);
connector.Connect(endpoint);

while (true)
{
    Console.Write("> ");
    var line = Console.ReadLine();
    if (line == "q")
    {
        break;
    }

    var msg = Packet.Create((short)PROTOCOL.CHAT_MSG_REQ);
    msg.Push(line);
    gameServers[0].Send(msg);
}

((RemoteServerPeer)gameServers[0]).Token.Disconnect();

Console.ReadKey();
