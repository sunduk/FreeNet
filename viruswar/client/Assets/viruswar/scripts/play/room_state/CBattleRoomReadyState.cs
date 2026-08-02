using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FreeNet;
using GameServer;

/// <summary>
/// Waiting state before the first turn starts.
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
                Vector2 map_position = new Vector3(j, i);
                clone.transform.localPosition = CHelper.map_to_world(map_position);

                // Save the index to distinguish which button was pressed.
                clone.AddComponent<CButtonAction>().set(index);
                ++index;
            }
        }
    }
}
