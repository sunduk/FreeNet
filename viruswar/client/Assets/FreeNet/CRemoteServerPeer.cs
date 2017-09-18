using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FreeNet;
using FreeNetUnity;

namespace FreeNetUnity
{
	public class CRemoteServerPeer : IPeer
	{
		public CUserToken token { get; private set; }
		WeakReference freenet_eventmanager;

		public CRemoteServerPeer(CUserToken token)
		{
			this.token = token;
			this.token.set_peer(this);
		}

		public void set_eventmanager(CFreeNetEventManager event_manager)
		{
			this.freenet_eventmanager = new WeakReference(event_manager);
		}

        public void update_heartbeat(float time)
        {
            this.token.update_heartbeat_manually(time);
        }

        /// <summary>
        /// 메시지를 수신했을 때 호출된다.
        /// </summary>
        void IPeer.on_message(CPacket msg)
		{
			(this.freenet_eventmanager.Target as CFreeNetEventManager).enqueue_network_message(msg);
		}

		void IPeer.on_removed()
		{
			(this.freenet_eventmanager.Target as CFreeNetEventManager).enqueue_network_event(NETWORK_EVENT.disconnected);
		}

		void IPeer.send(CPacket msg)
		{
			this.token.send(msg);
		}

		void IPeer.disconnect()
		{
            this.token.disconnect();
		}
	}
}
