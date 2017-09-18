using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using FreeNet;
using GameServer;

public class CBattleRoom : MonoBehaviour, IMessageReceiver {

    //--------------------------------------------------
    // Define state.
    // 상태 정의.
    //--------------------------------------------------
    public enum STATE
    {
        // 게임 시작 전 준비 상태.
        READY,

        // 내 턴이 진행중인 상태.
        TURN_PLAYING,

        // 상대방 턴이 진행중인 상태.
        WAIT,

        // 게임이 끝난 상태.
        GAMEOVER,
    }

    public enum MESSAGE
    {
        SHOW_RESULT,
    }



    //--------------------------------------------------
    // Reference data.
    // 참조용 데이터.
    //--------------------------------------------------
    // 가로, 세로 칸 수를 의미한다.
    public static readonly int COL_COUNT = 7;



    //--------------------------------------------------
    // Game instances.
    // 게임 객체들.
    //--------------------------------------------------
    // 플레이어들.
    List<CPlayer> players;

    // 점수등의 플레이어 정보.
    List<CPlayerGameInfoUI> players_gameinfo;

	// 현재 턴을 진행중인 플레이어 인덱스.
	byte current_player_index;

    // 서버에서 지정해준 본인의 플레이어 인덱스.
    byte player_me_index;

    // 승리한 플레이어 인덱스.
    // 무승부일때는 byte.MaxValue가 들어간다.
    byte win_player_index;

	// 게임이 종료되었는지를 나타내는 플래그.
	bool is_game_finished;

    // 상태 매니저.
    CStateManager state_manager;


    void Awake()
	{
        this.players = new List<CPlayer>();
        this.players_gameinfo = new List<CPlayerGameInfoUI>();

        // 방의 각 상태를 담당하는 인스턴스 생성.
        this.state_manager = gameObject.AddComponent<CStateManager>();
        this.state_manager.initialize(STATE_OBJECT_TYPE.ATTACH_TO_SINGLE_OBJECT);
        this.state_manager.add<CBattleRoomReadyState>(STATE.READY);
        this.state_manager.add<CBattleRoomWaitState>(STATE.WAIT);
        this.state_manager.add<CBattleRoomTurnPlayingState>(STATE.TURN_PLAYING);
        this.state_manager.add<CBattleRoomGameOverState>(STATE.GAMEOVER);

        // 초기 상태 설정.
        this.state_manager.change_state(STATE.READY);
    }


    /// <summary>
    /// Called when enter the game from client.
    /// Load resources if you need.
    /// 
    /// 게임방에 입장할 때 클라이언트에서 호출된다.
    /// 필요한 리소스가 있다면 여기서 로딩한다.
    /// </summary>
    public void start_loading()
    {
        clear_before_start();

        // From now on, this class instance will receives all network messages.
        // 네트워크에서 넘어온 메시지를 이 클래스 인스턴스가 받도록 설정한다.
        CNetworkManager.Instance.message_receiver = this;

        // Send ready.
        // 준비 완료 패킷 전송.
        CPacket msg = CPacket.create((short)PROTOCOL.READY_TO_START);
        CNetworkManager.Instance.send(msg);
    }


	void reset_ui()
	{
		this.players.ForEach(obj =>
		{
            refresh_score(obj);
		});

        this.players_gameinfo.ForEach(obj =>
        {
            obj.refresh_me(false);
            obj.refresh_turn(false);
        });

        this.players_gameinfo[this.player_me_index].refresh_me(true);
        this.players_gameinfo[this.current_player_index].refresh_turn(true);
    }


	void clear_before_start()
	{
		this.current_player_index = 0;
		this.is_game_finished = false;

        foreach (var player in this.players)
        {
            player.clear();
            player.GetComponent<CPlayerRenderer>().clear();
        }
        this.players.Clear();
        this.players_gameinfo.Clear();
    }


    /// <summary>
    /// Called when received packets.
    /// 패킷을 수신 했을 때 호출됨.
    /// </summary>
    /// <param name="protocol"></param>
    /// <param name="msg"></param>
    void IMessageReceiver.on_recv(CPacket msg)
	{
		PROTOCOL protocol_id = (PROTOCOL)msg.pop_protocol_id();

        // 동시접속자 정보가 아닌 다른 패킷일 수신했을 경우 WAIT팝업을 닫는다.
        if (protocol_id != PROTOCOL.CONCURRENT_USERS)
        {
            CUIManager.Instance.hide(UI_PAGE.POPUP_WAIT);
        }

		switch (protocol_id)
		{
                // 게임을 시작해라.
			case PROTOCOL.GAME_START:
				on_game_start(msg);
				break;

                // 플레이어가 이동 했다.
			case PROTOCOL.PLAYER_MOVED:
				on_player_moved(msg);
				break;

                // 턴을 시작해라.
			case PROTOCOL.START_PLAYER_TURN:
				on_start_player_turn(msg);
				break;

                // 방이 삭제됐다. 누가 끊겼던지 강제종료 했던지 등등.
            case PROTOCOL.ROOM_REMOVED:
                on_room_removed();
                break;

                // 게임이 종료됐다.
            case PROTOCOL.GAME_OVER:
				on_game_over(msg);
				break;

            case PROTOCOL.CONCURRENT_USERS:
                {
                    int count = msg.pop_int32();
                    CUIManager.Instance.get_uipage(UI_PAGE.STATUS_BAR).GetComponent<CStatusBar>().refresh(count);
                }
                break;
        }
	}


	void on_room_removed()
	{
        if (this.is_game_finished)
        {
            return;
        }

        this.is_game_finished = true;

        CUIManager.Instance.hide_all();
        CUIManager.Instance.show(UI_PAGE.POPUP_COMMON);
        CPopupCommon popup =
            CUIManager.Instance.get_uipage(UI_PAGE.POPUP_COMMON).GetComponent<CPopupCommon>();
        popup.refresh("상대방이 게임을 나갔습니다.", () => { back_to_main(); });
	}


    void destroy_all_resources()
    {
        this.players.ForEach(player => GameObject.Destroy(player.gameObject));
        this.players.Clear();
    }


	public void back_to_main()
	{
        destroy_all_resources();

        CUIManager.Instance.hide_all();
        CUIManager.Instance.show(UI_PAGE.MAIN_MENU);
        CUIManager.Instance.get_uipage(UI_PAGE.MAIN_MENU).GetComponent<CMainMenu>().enter();

        gameObject.SetActive(false);
	}


	void on_game_over(CPacket msg)
	{
		this.is_game_finished = true;
		this.win_player_index = msg.pop_byte();

        this.state_manager.change_state(STATE.GAMEOVER);
        this.state_manager.send_state_message(MESSAGE.SHOW_RESULT, 
            this.win_player_index, this.player_me_index);
	}


	void on_game_start(CPacket msg)
	{
        this.player_me_index = msg.pop_byte();

		byte count = msg.pop_byte();
		for (byte i = 0; i < count; ++i)
		{
			byte player_index = msg.pop_byte();

			GameObject obj = new GameObject(string.Format("player{0}", i));
			CPlayer player = obj.AddComponent<CPlayer>();
			player.initialize(player_index);
			player.clear();

            obj.AddComponent<CPlayerRenderer>().initialize(player);

            byte virus_count = msg.pop_byte();
			for (byte index = 0; index < virus_count; ++index)
			{
				short position = msg.pop_int16();
                clone(player, position);
			}

			this.players.Add(player);

            // Make player score board.
            Transform gameinfo = 
                transform.FindChild(string.Format("player_info_{0:D2}", (player_index + 1)));
            this.players_gameinfo.Add(gameinfo.gameObject.AddComponent<CPlayerGameInfoUI>());
		}

		this.current_player_index = msg.pop_byte();

        this.win_player_index = byte.MaxValue;
        reset_ui();
        start_turn();
	}


    public bool is_my_turn()
    {
        return this.current_player_index == this.player_me_index;
    }


    void start_turn()
    {
        if (is_my_turn())
        {
            this.state_manager.change_state(STATE.TURN_PLAYING);
        }
        else
        {
            this.state_manager.change_state(STATE.WAIT);
        }
    }


    void remove(CPlayer player, short position)
    {
        player.remove(position);
        player.GetComponent<CPlayerRenderer>().remove(position);
    }


    void clone(CPlayer player, short position)
    {
        player.add(position);
        player.GetComponent<CPlayerRenderer>().add(position);
    }


	void on_player_moved(CPacket msg)
	{
		byte player_index = msg.pop_byte();
		short from = msg.pop_int16();
		short to = msg.pop_int16();

		StartCoroutine(on_selected_cell_to_attack(player_index, from, to));
	}


	void on_start_player_turn(CPacket msg)
	{
		this.current_player_index = msg.pop_byte();
        start_turn();

        this.players_gameinfo.ForEach(obj => obj.refresh_turn(false));
        this.players_gameinfo[this.current_player_index].refresh_turn(true);
    }


	IEnumerator on_selected_cell_to_attack(byte player_index, short from, short to)
	{
		byte distance = CHelper.howfar_from_clicked_cell(from, to);
		if (distance == 1)
		{
            // copy to cell
            clone(this.players[player_index], to);
		}
		else if (distance == 2)
		{
			// move
            remove(this.players[player_index], from);
            clone(this.players[player_index], to);
        }

        refresh_score(this.players[player_index]);
        yield return StartCoroutine(reproduce(to));

		CPacket msg = CPacket.create((short)PROTOCOL.TURN_FINISHED_REQ);
        CNetworkManager.Instance.send(msg);

		yield return 0;
	}
	

    void refresh_score(CPlayer player)
    {
        this.players_gameinfo[player.player_index].refresh_score(player.get_virus_count());
    }


	IEnumerator reproduce(short center)
	{
        yield return new WaitForSeconds(0.2f);

        CPlayer current_player = this.players[this.current_player_index];
		CPlayer other_player = this.players.Find(obj => obj.player_index != this.current_player_index);

		// eat.
		List<short> neighbors = CHelper.find_neighbor_cells(center, other_player.cell_indexes, 1);
		foreach (short obj in neighbors)
		{
            clone(current_player, obj);
            remove(other_player, obj);

            refresh_score(current_player);
            refresh_score(other_player);

            yield return new WaitForSeconds(0.2f);
		}
	}


    public CPlayer get_current_player()
    {
        return this.players[this.current_player_index];
    }


    public List<CPlayer> get_players()
    {
        return this.players;
    }


    public bool is_finished()
    {
        return this.is_game_finished;
    }
}
