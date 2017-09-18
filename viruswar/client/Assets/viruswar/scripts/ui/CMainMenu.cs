using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using FreeNet;
using GameServer;

public class CMainMenu : MonoBehaviour, IMessageReceiver
{
    enum USER_STATE
    {
        NOT_CONNECTED,
        CONNECTED,
        WAITING_MATCHING
    }

    CNetworkManager network_manager;
    USER_STATE user_state;


    void Awake()
    {
        this.user_state = USER_STATE.NOT_CONNECTED;
        this.network_manager = GameObject.FindObjectOfType<CNetworkManager>();
        this.user_state = USER_STATE.NOT_CONNECTED;

        transform.FindChild("button_start").GetComponent<Button>().onClick.AddListener(this.on_play);
    }

    public void enter()
    {
        this.network_manager.message_receiver = this;
    }


    void on_play()
    {
        if (!this.network_manager.is_connected())
        {
            CUIManager.Instance.hide_all();
            CUIManager.Instance.show(UI_PAGE.POPUP_NETWORK_PROCESSING);
            CPopupNetworkProcessing popup =
                CUIManager.Instance.get_uipage(UI_PAGE.POPUP_NETWORK_PROCESSING).GetComponent<CPopupNetworkProcessing>();
            popup.refresh("서버에 접속중");

            this.network_manager.connect();
        }

        CPacket msg = CPacket.create((short)PROTOCOL.ENTER_GAME_ROOM_REQ);
        this.network_manager.send(msg);
    }


    /// <summary>
    /// 패킷을 수신 했을 때 호출됨.
    /// </summary>
    /// <param name="protocol"></param>
    /// <param name="msg"></param>
    void IMessageReceiver.on_recv(CPacket msg)
    {
        // 제일 먼저 프로토콜 아이디를 꺼내온다.
        PROTOCOL protocol_id = (PROTOCOL)msg.pop_protocol_id();

        switch (protocol_id)
        {
            case PROTOCOL.ENTER_GAME_ROOM_ACK:
                {
                    CUIManager.Instance.hide_all();
                    CUIManager.Instance.show(UI_PAGE.POPUP_NETWORK_PROCESSING);
                    CPopupNetworkProcessing popup =
                        CUIManager.Instance.get_uipage(UI_PAGE.POPUP_NETWORK_PROCESSING).GetComponent<CPopupNetworkProcessing>();
                    popup.refresh("매칭 대기중");

                    CUIManager.Instance.show(UI_PAGE.STATUS_BAR);
                    CUIManager.Instance.get_uipage(UI_PAGE.STATUS_BAR).GetComponent<CStatusBar>().refresh(1);
                }
                break;

            case PROTOCOL.START_LOADING:
                {
                    CUIManager.Instance.hide_all();
                    CUIManager.Instance.show(UI_PAGE.PLAY_ROOM);
                    CBattleRoom room =
                        CUIManager.Instance.get_uipage(UI_PAGE.PLAY_ROOM).GetComponent<CBattleRoom>();
                    room.start_loading();

                    CUIManager.Instance.show(UI_PAGE.STATUS_BAR);
                }
                break;

            case PROTOCOL.CONCURRENT_USERS:
                {
                    int count = msg.pop_int32();
                    CUIManager.Instance.get_uipage(UI_PAGE.STATUS_BAR).GetComponent<CStatusBar>().refresh(count);
                }
                break;
        }
    }


    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CUIManager.Instance.show(UI_PAGE.POPUP_QUIT);
            CPopupQuit popup =
                CUIManager.Instance.get_uipage(UI_PAGE.POPUP_QUIT).GetComponent<CPopupQuit>();
            popup.refresh(() =>
            {
                if (Application.platform == RuntimePlatform.Android ||
                    Application.platform == RuntimePlatform.IPhonePlayer)
                {
                    Application.Quit();
                }
            });
        }
    }
}
