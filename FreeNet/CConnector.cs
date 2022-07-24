using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace FreeNet
{
	/// <summary>
	/// Endpoint정보를 받아서 서버에 접속한다.
	/// 접속하려는 서버 하나당 인스턴스 한개씩 생성하여 사용하면 된다.
	/// </summary>
	public class CConnector
	{
		public delegate void ConnectedHandler(CUserToken token);
		public ConnectedHandler connected_callback { get; set; }

		// 원격지 서버와의 연결을 위한 소켓.
		Socket client;

		CNetworkService network_service;

		public CConnector(CNetworkService network_service)
		{
			this.network_service = network_service;
			this.connected_callback = null;
		}

		public void connect(IPEndPoint remote_endpoint)
		{
			this.client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.client.NoDelay = true;

			// 비동기 접속을 위한 event args.
			SocketAsyncEventArgs event_arg = new SocketAsyncEventArgs();
			event_arg.Completed += on_connect_completed;
			event_arg.RemoteEndPoint = remote_endpoint;
			bool pending = this.client.ConnectAsync(event_arg);
			if (!pending)
			{
				on_connect_completed(null, event_arg);
			}
		}

		void on_connect_completed(object sender, SocketAsyncEventArgs e)
		{
			if (e.SocketError == SocketError.Success)
			{
				//Console.WriteLine("Connect completd!");
				// 여기서 token은 현재 접속한 원격지 '서버'를 의미한다.
				CUserToken token = new CUserToken(this.network_service.logic_entry);

				// 1) 어플리케이션 코드로 '접속 완료' 콜백을 전달한다.
				// 반드시 아래 on_connect_completed함수가 수행되기 전에 호출되어야 한다.
				// 네트웍 코드로부터 패킷 수신 처리가 수행되기 전에 어플리케이션 코드에서 모든 준비를 마쳐놓고 기다려야 하기 때문이다.
				// 만약 2)번이 먼저 수행되고 그 다음 1)번이 수행된다면 네트웍 코드에서 수신한 패킷을 어플리케이션에서 받아가지 못할 상황이 발생할 수 있다.
				if (this.connected_callback != null)
				{
					this.connected_callback(token);
				}

				// 2) 데이터 수신 준비.
				// 아래 함수가 호출된 직후부터 패킷 수신이 가능하다.
				// 딜레이 없이 즉시 패킷 수신 처리가 이루어 질 수 있으므로
				// 어플리케이션쪽 코드에서는 네트웍 코드가 넘겨준 패킷을 처리할 수 있는 상태여야 한다.
				this.network_service.on_connect_completed(this.client, token);
			}
			else
			{
				// failed.
				Console.WriteLine(string.Format("Failed to connect. {0}", e.SocketError));
			}
		}
	}
}
