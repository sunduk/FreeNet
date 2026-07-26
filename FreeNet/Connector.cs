using System;
using System.Net;
using System.Net.Sockets;

namespace FreeNet;

/// <summary>
/// Endpoint정보를 받아서 서버에 접속한다. 접속하려는 서버 하나당 인스턴스 한개씩 생성하여 사용하면 된다.
/// </summary>
public class Connector(NetworkService network_service)
{
    /// <summary>
    /// 원격지 서버와의 연결을 위한 소켓.
    /// </summary>
    private Socket _client;

    /// <summary>
    /// 접속 완료시 호출되는 콜백 함수 정의
    /// </summary>
    /// <param name="token">The token.</param>
    public delegate void ConnectedHandler(UserToken token);

    /// <summary>
    /// Gets or sets the connected callback.
    /// </summary>
    /// <value>The connected callback.</value>
    public ConnectedHandler ConnectedCallback { get; set; } = null;

    /// <summary>
    /// Connects to the specified remote endpoint.
    /// </summary>
    /// <param name="remote_endpoint">The remote endpoint.</param>
    public void Connect(IPEndPoint remote_endpoint)
    {
        _client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
        {
            NoDelay = true
        };

        // 비동기 접속을 위한 event args.
        SocketAsyncEventArgs event_arg = new();
        event_arg.Completed += OnConnectCompleted;
        event_arg.RemoteEndPoint = remote_endpoint;
        bool pending = _client.ConnectAsync(event_arg);
        if (!pending)
        {
            OnConnectCompleted(this, event_arg);
        }
    }

    /// <summary>
    /// Called when the connect operation is completed.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="SocketAsyncEventArgs"/> instance containing the event data.</param>
    private void OnConnectCompleted(object sender, SocketAsyncEventArgs e)
    {
        if (e.SocketError == SocketError.Success)
        {
            //Console.WriteLine("Connect completd!");
            // 여기서 token은 현재 접속한 원격지 '서버'를 의미한다.
            UserToken token = new(network_service.LogicEntry);

            // 1) 어플리케이션 코드로 '접속 완료' 콜백을 전달한다. 반드시 아래 OnConnectCompleted함수가 수행되기 전에 호출되어야 한다. 네트웍
            // 코드로부터 패킷 수신 처리가 수행되기 전에 어플리케이션 코드에서 모든 준비를 마쳐놓고 기다려야 하기 때문이다. 만약 2)번이 먼저 수행되고 그 다음
            // 1)번이 수행된다면 네트웍 코드에서 수신한 패킷을 어플리케이션에서 받아가지 못할 상황이 발생할 수 있다.
            ConnectedCallback?.Invoke(token);

            // 2) 데이터 수신 준비. 아래 함수가 호출된 직후부터 패킷 수신이 가능하다. 딜레이 없이 즉시 패킷 수신 처리가 이루어 질 수 있으므로 어플리케이션쪽
            // 코드에서는 네트웍 코드가 넘겨준 패킷을 처리할 수 있는 상태여야 한다.
            network_service.OnConnectCompleted(_client, token);
        }
        else
        {
            // failed.
            Console.WriteLine(string.Format("Failed to connect. {0}", e.SocketError));
        }
    }
}