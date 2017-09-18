using UnityEngine;
using System.Collections;

public class CMapCollision : MonoBehaviour {

	void Update()
	{
        if (Input.GetMouseButtonDown(0))
		{
			RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit))
			{
                GameObject target = hit.transform.gameObject;
                gameObject.SendMessage("on_touch_collision_area", target);
			}
		}
	}
}
