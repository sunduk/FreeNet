using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 객체들을 품고 있는 월드 객체.
/// </summary>
public class CGameWorld : CSingletonMonobehaviour<CGameWorld>
{
    /// <summary>
    /// 월드내에 객체를 생성한다.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public GameObject instantiate(GameObject obj)
    {
        // 객체 생성시 CGameWorld하위로 오도록 만든다.
        // 어떤 오브젝트가 어디에 있는지 디버깅하기 쉬우라고 이렇게 했음.
        GameObject clone = GameObject.Instantiate(obj);
        clone.transform.parent = transform;
        return clone;
    }
}
