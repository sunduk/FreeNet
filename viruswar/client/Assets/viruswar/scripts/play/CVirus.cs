using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 바이러스 객체.
/// </summary>
public class CVirus : MonoBehaviour {

    // 맵 포지션.
    public short cell { get; private set; }

    GameObject appear;
    GameObject disappear;


    void Awake()
    {
        // 생성될 때 사용할 오브젝트.
        this.appear = transform.FindChild("appear").gameObject;
        this.appear.SetActive(false);

        // 사라질 때 사용할 오브젝트.
        this.disappear = transform.FindChild("destroy").gameObject;
        this.disappear.SetActive(false);
    }


    public void update_position(short cell)
    {
        this.cell = cell;
    }


    /// <summary>
    /// 대기 상태로 만든다.
    /// </summary>
    public void idle()
    {
        // 터치 불가능 하게 한다.
        GetComponent<BoxCollider>().enabled = false;

        this.appear.SetActive(true);
        // 모션을 멈춘다.
        this.appear.GetComponent<CRotator>().stop();
    }


    /// <summary>
    /// 터치 가능한 상태로 만든다.
    /// </summary>
    public void touchable()
    {
        GetComponent<BoxCollider>().enabled = true;
    }


    /// <summary>
    /// 삭제 한다.
    /// </summary>
    public void destroy()
    {
        this.appear.SetActive(false);
        this.disappear.SetActive(true);
    }


    public void on_touch()
    {
        // 좌, 우로 흔들거리는 모습 재생.
        this.appear.GetComponent<CRotator>().play();
    }


    public bool is_same(short cell)
    {
        return this.cell == cell;
    }
}
