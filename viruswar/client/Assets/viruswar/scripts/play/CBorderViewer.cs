using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This class shows borders of movable cells for a player.
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
        // Maximum area a single object can move.
        const int MAX_MOVABLE_CELL_COUNT = 24;

        // Load resources.
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
            // Convert map coordinates to world coordinates and apply to transform.
            Vector3 pos = CHelper.map_to_world(CHelper.convert_to_position(targets[i]));
            this.borders[i].transform.position = pos;
            this.borders[i].SetActive(true);

            if (CHelper.howfar_from_clicked_cell(center, targets[i]) <= 1)
            {
                // Image to mark cells one cell away.
                this.borders[i].transform.FindChild("copy").gameObject.SetActive(true);
                this.borders[i].transform.FindChild("move").gameObject.SetActive(false);
            }
            else
            {
                // Image to mark cells two cells away.
                this.borders[i].transform.FindChild("move").gameObject.SetActive(true);
                this.borders[i].transform.FindChild("copy").gameObject.SetActive(false);
            }
        }
    }
}
