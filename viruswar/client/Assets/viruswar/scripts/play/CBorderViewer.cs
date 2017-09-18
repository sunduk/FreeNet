using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This class shows borders of movable cells for a player.
/// 이 클래스는 캐릭터가 이동할 수 있는 셀을 보여주는 기능을 한다.
/// </summary>
public class CBorderViewer : MonoBehaviour {

    List<GameObject> borders;


    void Awake()
    {
        this.borders = new List<GameObject>();

        load();
    }


    void load()
    {
        // 하나의 객체가 이동 할 수 있는 최대 영역.
        const int MAX_MOVABLE_CELL_COUNT = 24;

        // 리소스 로딩.
        this.borders.Clear();
        GameObject source = Resources.Load("prefabs/border") as GameObject;
        for (int i = 0; i < MAX_MOVABLE_CELL_COUNT; ++i)
        {
            GameObject clone = CGameWorld.Instance.instantiate(source);
            clone.SetActive(false);
            this.borders.Add(clone);
        }
    }


    public void hide()
    {
        for (int i = 0; i < this.borders.Count; ++i)
        {
            this.borders[i].SetActive(false);
        }
    }


    public void show(short center, List<short> targets)
    {
        for (int i = 0; i < targets.Count; ++i)
        {
            // 맵 좌표를 월드 좌표로 변환하여 트랜스폼에 적용시킨다.
            Vector3 pos = CHelper.map_to_world(CHelper.convert_to_position(targets[i]));
            this.borders[i].transform.position = pos;
            this.borders[i].SetActive(true);

            if (CHelper.howfar_from_clicked_cell(center, targets[i]) <= 1)
            {
                // 한칸 떨어진 곳을 표시할 이미지.
                this.borders[i].transform.FindChild("copy").gameObject.SetActive(true);
                this.borders[i].transform.FindChild("move").gameObject.SetActive(false);
            }
            else
            {
                // 두칸 떨어진 곳을 표시할 이미지.
                this.borders[i].transform.FindChild("move").gameObject.SetActive(true);
                this.borders[i].transform.FindChild("copy").gameObject.SetActive(false);
            }
        }
    }
}
