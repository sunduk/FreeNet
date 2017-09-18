using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 상대방 턴이 진행중이라 대기중인 상태.
/// </summary>
public class CBattleRoomWaitState : MonoBehaviour, IState
{
    CBattleRoom room;


    void Awake()
    {
        this.room = GetComponent<CBattleRoom>();
    }


    void IState.on_enter()
    {
        disable_cell_touch();
        GetComponent<CBorderViewer>().hide();

        StartCoroutine(routine_wait());
    }


    void IState.on_exit()
    {
    }


    void disable_cell_touch()
    {
        this.room.get_players().ForEach(player => player.GetComponent<CPlayerRenderer>().idle());
    }


    IEnumerator routine_wait()
    {
        yield return new WaitForSeconds(5.0f);

        if (!this.room.is_finished())
        {
            CUIManager.Instance.show(UI_PAGE.POPUP_WAIT);
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
                CNetworkManager.Instance.disconnect();
            });
        }
    }
}
