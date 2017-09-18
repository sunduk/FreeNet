using UnityEngine;
using System;
using System.Collections;

public enum STATE_OBJECT_TYPE
{
    // 자신의 오브젝트에 모든 스테이트 스크립트를 attach하는 형태.
    ATTACH_TO_SINGLE_OBJECT,

    // 새로운 게임 오브젝트를 생성하고 child로 붙이는 형태.
    CREATE_NEW_OBJECT
}


/// <summary>
/// 스테이트 생성 방식에 따른 분류.
/// </summary>
public interface IStateObjectGenerationType
{
    IState make_state_object<T>(GameObject parent, Enum key) where T : Component, IState;
    void set_active(IState obj, bool flag);
}
