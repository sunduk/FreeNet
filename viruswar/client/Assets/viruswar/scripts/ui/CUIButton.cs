using UnityEngine;
using System.Collections;

public class CUIButton : MonoBehaviour {

	public GameObject target { get; private set; }
	public string method_name { get; private set; }


	public void set_touch_handler(GameObject target, string method_name)
	{
		this.target = target;
		this.method_name = method_name;
	}


	public void on_touch()
	{
        this.target.SendMessage(this.method_name);
	}
}
