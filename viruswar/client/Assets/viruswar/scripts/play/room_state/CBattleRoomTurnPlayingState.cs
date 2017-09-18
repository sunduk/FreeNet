using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FreeNet;
using GameServer;

/// <summary>
/// 내 턴이 진행중인 상태.
/// </summary>
public class CBattleRoomTurnPlayingState : MonoBehaviour, IState
{
    CBattleRoom room;

    // The player's character position that selected.
    // 선택한 캐릭터의 위치.
    short selected_character_position = short.MaxValue;

    // A Board data contains indexes from 0 to 49.
    // 0~49까지의 인덱스를 갖고 있는 보드판 데이터.
    List<short> table_board;


    void Awake()
    {
        this.room = GetComponent<CBattleRoom>();

        // Make board data.
        // 보드판 데이터를 만든다.
        this.table_board = new List<short>();
        for (int i = 0; i < CBattleRoom.COL_COUNT * CBattleRoom.COL_COUNT; ++i)
        {
            this.table_board.Add((short)i);
        }

        // Enable touches.
        // 터치 활성화.
        gameObject.AddComponent<CMapCollision>();

        // A component to see movable area.
        // 이동 가능한 영역을 보여주기 위한 컴포넌트.
        gameObject.AddComponent<CBorderViewer>();
    }


    void IState.on_enter()
    {
        ready_to_select();
    }


    void IState.on_exit()
    {
        // Disable collision check.
        gameObject.GetComponent<CMapCollision>().enabled = false;
    }


    void ready_to_select()
    {
        GetComponent<CBorderViewer>().hide();

        // Enable collision check.
        // 충돌 기능 활성화.
        gameObject.GetComponent<CMapCollision>().enabled = true;

        // Stop effects.
        this.room.get_players().ForEach(player => player.GetComponent<CPlayerRenderer>().stop());

        // Enable viruses touch if my turn playing.
        // 내 턴일 경우 바이러스들의 터치를 활성화 한다.
        if (this.room.is_my_turn())
        {
            this.room.get_current_player().GetComponent<CPlayerRenderer>().ready();
        }
    }


    /// <summary>
    /// Called when collision area touched.
    /// 충돌영역 어딘가에 터치 이벤트가 발생 했을 때.
    /// </summary>
    /// <param name="target"></param>
    void on_touch_collision_area(GameObject target)
    {
        // When touched a character.
        // 캐릭터를 터치했을 때.
        CVirus virus = target.GetComponent<CVirus>();
        if (virus != null)
        {
            this.selected_character_position = virus.cell;

            this.room.get_current_player().GetComponent<CPlayerRenderer>().stop();
            virus.on_touch();

            show_movable_area(virus.cell);
            return;
        }

        // When touched an empty cell.
        // 빈 셀을 터치했을 때.
        CButtonAction cell = target.GetComponent<CButtonAction>();
        if (cell != null)
        {
            on_cell_touch((short)cell.index);
            return;
        }
    }


    void show_movable_area(short center)
    {
        List<short> targets =
            CHelper.find_available_cells(center, this.table_board, this.room.get_players());

        GetComponent<CBorderViewer>().hide();
        GetComponent<CBorderViewer>().show(center, targets);
    }


    /// <summary>
    /// When touched cell area.
    /// 셀 영역을 터치 했을 때.
    /// </summary>
    /// <param name="cell"></param>
    void on_cell_touch(short cell)
    {
        // An opponent place can not be touched.
        // 상대방이 있는 자리는 터치할 수 없다.
        foreach (CPlayer player in this.room.get_players())
        {
            if (player.cell_indexes.Exists(obj => obj == cell))
            {
                return;
            }
        }

        // A distance over two space can not be moved.
        // 2칸을 초과하는 거리는 이동할 수 없다.
        if (CHelper.get_distance(this.selected_character_position, cell) > 2)
        {
            return;
        }

        GetComponent<CBorderViewer>().hide();

        // Send moving packet.
        // 이동 패킷 전송.
        CPacket msg = CPacket.create((short)PROTOCOL.MOVING_REQ);
        msg.push(this.selected_character_position);
        msg.push(cell);
        CNetworkManager.Instance.send(msg);

        GetComponent<CStateManager>().change_state(CBattleRoom.STATE.WAIT);
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
