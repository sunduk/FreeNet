using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FreeNet;
using GameServer;

/// <summary>
/// 첫번째 턴 시작 전 대기 상태.
/// </summary>
public class CBattleRoomReadyState : MonoBehaviour, IState
{
    void Awake()
    {
        make_touchable_buttons();
    }


    void IState.on_enter()
    {
        GetComponent<CBorderViewer>().hide();
    }


    void IState.on_exit()
    {
    }


    void make_touchable_buttons()
    {
        GameObject source = Resources.Load("prefabs/touchable_area") as GameObject;

        int index = 0;
        for (int i = 0; i < CBattleRoom.COL_COUNT; ++i)
        {
            for (int j = 0; j < CBattleRoom.COL_COUNT; ++j)
            {
                GameObject clone = CGameWorld.Instance.instantiate(source);

                // Convert map position to world position.
                // 맵 좌표를 월드 좌표로 변환한다.
                Vector2 map_position = new Vector3(j, i);
                clone.transform.localPosition = CHelper.map_to_world(map_position);

                // Set button index to find which button is touched.
                // 어느 버튼을 눌렀는지 구별하기 위한 인덱스를 저장한다.
                clone.AddComponent<CButtonAction>().set(index);
                ++index;
            }
        }
    }
}
