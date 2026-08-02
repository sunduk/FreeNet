using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using FreeNet;

namespace FreeNetUnity
{
	/// <summary>
	/// This class bridges the FreeNet engine and Unity application.
	/// It receives connection events and message reception events from the FreeNet engine
	/// and forwards them to the application. It inherits from MonoBehaviour and is implemented
	/// to operate on the same thread as the Unity application.
	/// Therefore, no additional synchronization is needed when accessing Unity objects
	/// in this class's callback methods.
	/// </summary>
	public class CFreeNetUnityService : MonoBehaviour
	{
		CFreeNetEventManager event_manager;

		// Connected game server object.
		IPeer gameserver;

		// Service object for TCP communication.
		CNetworkService service;

		// Delegate called when connection is established. The application sets a callback method to use.
		public delegate void StatusChangedHandler(NETWORK_EVENT status);
		public StatusChangedHandler appcallback_on_status_changed;

		// Delegate called when network message is received. The application sets a callback method to use.
		public delegate void MessageHandler(CPacket msg);
		public MessageHandler appcallback_on_message;

		void Awake()
		{
			this.event_manager = new CFreeNetEventManager();
        }

		public void connect(string host, int port)
		{
			if (this.service == null)
			{
				// CNetworkService object handles asynchronous message send/receive processing.
				this.service = new CNetworkService();
			}

			// Create a Connector with endpoint information. Pass the NetworkService object created above.
			CConnector connector = new CConnector(service);
			// Specify the callback method to be called when connection is successful.
			connector.connected_callback += on_connected_gameserver;
			IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse(host), port);
			connector.connect(endpoint);
		}


		public bool is_connected()
		{
			return this.gameserver != null;
		}


		/// <summary>
		/// Callback method called when connection is successful.
		/// </summary>
		/// <param name="server_token"></param>
		void on_connected_gameserver(CUserToken server_token)
		{
			this.gameserver = new CRemoteServerPeer(server_token);
			((CRemoteServerPeer)this.gameserver).set_eventmanager(this.event_manager);

			// Disable heartbeat from the engine since it will be sent directly from Update method.
			server_token.disable_auto_heartbeat();

			// Queue the event to the manager to pass it to the Unity application.
			this.event_manager.enqueue_network_event(NETWORK_EVENT.connected);
		}

		/// <summary>
		/// All network events are reported to the client in the Update method.
		/// Message send/receive processing in the FreeNet engine is performed on worker threads,
		/// but logic processing in Unity is performed on the main thread,
		/// so through queuing, all logic processing is performed on the main thread.
		/// </summary>
		void Update()
		{
			// Callback for received messages.
			if (this.event_manager.has_message())
			{
				CPacket msg = this.event_manager.dequeue_network_message();
				if (this.appcallback_on_message != null)
				{
					this.appcallback_on_message(msg);
				}
			}

			// Callback for network events.
			if (this.event_manager.has_event())
			{
				NETWORK_EVENT status = this.event_manager.dequeue_network_event();
				on_status_changed(status);
				if (this.appcallback_on_status_changed != null)
				{
					this.appcallback_on_status_changed(status);
				}
			}

			// Heartbeat.
			if (this.gameserver != null)
			{
				((CRemoteServerPeer)this.gameserver).update_heartbeat(Time.deltaTime);
			}
		}


        void on_status_changed(NETWORK_EVENT status)
        {
            switch (status)
            {
                case NETWORK_EVENT.disconnected:
                    this.gameserver = null;
                    break;
            }
        }


		public void send(CPacket msg)
		{
			try
			{
				this.gameserver.send(msg);
			}
			catch (Exception e)
			{
				Debug.LogError(e.Message);
			}
		}

		/// <summary>
		/// On normal shutdown, disconnect must be called from the OnApplicationQuit method
		/// to prevent Unity from hanging.
		/// </summary>
		void OnApplicationQuit()
		{
			if (this.gameserver != null)
			{
				((CRemoteServerPeer)this.gameserver).token.disconnect();
			}
		}


        public void disconnect()
        {
            if (this.gameserver != null)
            {
                this.gameserver.disconnect();
            }
        }
	}

}
