using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using FreeNet;
using FreeNetUnity;

public interface IMessageReceiver
{
    void on_recv(CPacket msg);
}

public class CNetworkManager : CSingletonMonobehaviour<CNetworkManager>
{
    [SerializeField]
    string server_ip;

    [SerializeField]
    string server_port;

    Queue<CPacket> sending_queue;
    CFreeNetUnityService freenet;
	public IMessageReceiver message_receiver;


	void Awake()
	{
        this.freenet = gameObject.AddComponent<CFreeNetUnityService>();
        this.freenet.appcallback_on_message += this.on_message;
        this.freenet.appcallback_on_status_changed += this.on_status_changed;

        this.sending_queue = new Queue<CPacket>();
	}


    public void connect()
    {
        // 이전에 보내지 못한 패킷은 모두 버린다.
        this.sending_queue.Clear();

        if (!this.freenet.is_connected())
        {
            this.freenet.connect(this.server_ip, int.Parse(this.server_port));
        }
    }


    public void disconnect()
    {
        if (is_connected())
        {
            this.freenet.disconnect();
            return;
        }

        back_to_main();
    }


    void on_message(CPacket msg)
	{
		this.message_receiver.on_recv(msg);
	}


    void on_status_changed(NETWORK_EVENT status)
    {
        switch (status)
        {
            case NETWORK_EVENT.disconnected:
                back_to_main();
                break;
        }
    }


    void back_to_main()
    {
        CUIManager.Instance.hide_all();
        CUIManager.Instance.show(UI_PAGE.MAIN_MENU);
        CUIManager.Instance.get_uipage(UI_PAGE.MAIN_MENU).GetComponent<CMainMenu>().enter();
    }


    public void send(CPacket msg)
	{
        this.sending_queue.Enqueue(msg);
	}


    void Update()
    {
        if (!this.freenet.is_connected())
        {
            return;
        }

        while (this.sending_queue.Count > 0)
        {
            CPacket msg = this.sending_queue.Dequeue();
            this.freenet.send(msg);
        }
    }


    public bool is_connected()
    {
        if (this.freenet == null)
        {
            return false;
        }

        return this.freenet.is_connected();
    }
}
