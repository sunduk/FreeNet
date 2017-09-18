using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FreeNet;
using GameServer;

/// <summary>
/// 게임이 종료된 상태.
/// </summary>
public class CBattleRoomGameOverState : MonoBehaviour, IState
{
    void Awake()
    {
        GetComponent<CStateManager>().register_message_handler(this, CBattleRoom.MESSAGE.SHOW_RESULT, this.on_show_result);
    }


    void IState.on_enter()
    {
        GetComponent<CBorderViewer>().hide();
    }


    void IState.on_exit()
    {
    }


    void on_show_result(params object[] args)
    {
        byte win_player_index = (byte)args[0];
        byte player_me_index = (byte)args[1];

        CUIManager.Instance.show(UI_PAGE.GAME_RESULT);
        CPopupResult result =
            CUIManager.Instance.get_uipage(UI_PAGE.GAME_RESULT).GetComponent<CPopupResult>();
        result.refresh(win_player_index, player_me_index);
    }


    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (CUIManager.Instance.get_uipage(UI_PAGE.GAME_RESULT).activeSelf)
            {
                CUIManager.Instance.get_uipage(UI_PAGE.GAME_RESULT).GetComponent<CPopupResult>().on_ok();
                return;
            }

            // 종료 팝업 출력.
            CUIManager.Instance.show(UI_PAGE.POPUP_QUIT);
            CPopupQuit popup =
                CUIManager.Instance.get_uipage(UI_PAGE.POPUP_QUIT).GetComponent<CPopupQuit>();
            popup.refresh(() =>
            {
                CNetworkManager.Instance.disconnect();
            });
        }
    }
}
