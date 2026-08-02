using CSampleClient;
using FreeNet;
using Protocol;
using System.Net;

PacketBufferManager.Initialize(2000);

// The NetworkService object handles asynchronous message sending and receiving.
// Because the same logic can be used by both the server and client, create a NetworkService object and pass it to the Connector.
NetworkService service = new(true);

// Create a Connector with the endpoint information and provide the NetworkService object you created.
Connector connector = new(service);

var uri = "sam.gamebass.net";
var addresses = Dns.GetHostAddresses(uri);

foreach (var address in addresses)
{
    Console.WriteLine(address.ToString());
}

List<IPeer> gameServers = [];

// Register the callback method that will be invoked when the connection succeeds.
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

var endpoint = new IPEndPoint(addresses[0], 3369);
await connector.ConnectAsync(endpoint);

while (true)
{
    Console.Write("> ");
    var line = Console.ReadLine();
    if (line == "q")
    {
        break;
    }

    if (line.StartsWith("move"))
    {
        var msg = Packet.Create((short)PacketProtocol.MOVE_REQ);
        msg.Push(1.0f); // x
        msg.Push(2.0f); // y
        msg.Push(3.0f); // z
        msg.Push(4.0f); // r
        gameServers[0].Send(msg);
    }
    else
    {
        //CPacket msg = CPacket.create((short)EPacketProtocol.CHAT_MSG_REQ);
        //msg.push(line);
        //gameServers[0].Send(msg);
    }
}

((RemoteServerPeer)gameServers[0]).Token.Disconnect();

Console.ReadKey();
