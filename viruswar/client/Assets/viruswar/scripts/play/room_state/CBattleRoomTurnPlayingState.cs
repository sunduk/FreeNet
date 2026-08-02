using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FreeNet;
using GameServer;

/// <summary>
/// State when my turn is in progress.
/// </summary>
public class CBattleRoomTurnPlayingState : MonoBehaviour, IState
{
    CBattleRoom room;

    // The player's character position that selected.
    short selected_character_position = short.MaxValue;

    // A Board data contains indexes from 0 to 49.
    List<short> table_board;


    void Awake()
    {
        this.room = GetComponent<CBattleRoom>();

        // Make board data.
        this.table_board = new List<short>();
        for (int i = 0; i < CBattleRoom.COL_COUNT * CBattleRoom.COL_COUNT; ++i)
        {
            this.table_board.Add((short)i);
        }

        // Enable touches.
        gameObject.AddComponent<CMapCollision>();

        // A component to see movable area.
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
        gameObject.GetComponent<CMapCollision>().enabled = true;

        // Stop effects.
        this.room.get_players().ForEach(player => player.GetComponent<CPlayerRenderer>().stop());

        // Enable viruses touch if my turn playing.
        if (this.room.is_my_turn())
        {
            this.room.get_current_player().GetComponent<CPlayerRenderer>().ready();
        }
    }


    /// <summary>
    /// Called when collision area touched.
    /// </summary>
    /// <param name="target"></param>
    void on_touch_collision_area(GameObject target)
    {
        // When touched a character.
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
    /// </summary>
    /// <param name="cell"></param>
    void on_cell_touch(short cell)
    {
        // An opponent place can not be touched.
        foreach (CPlayer player in this.room.get_players())
        {
            if (player.cell_indexes.Exists(obj => obj == cell))
            {
                return;
            }
        }

        // A distance over two spaces can not be moved.
        if (CHelper.get_distance(this.selected_character_position, cell) > 2)
        {
            return;
        }

        GetComponent<CBorderViewer>().hide();

        // Send moving packet.
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
