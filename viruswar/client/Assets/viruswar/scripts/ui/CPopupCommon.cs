using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using FreeNet;
using GameServer;

public class CPopupCommon : MonoBehaviour
{
    System.Action callback = delegate { };
    Text txt_msg;


    void Awake()
    {
        this.txt_msg = transform.FindChild("msg").GetComponent<Text>();

        Transform button_ok = transform.FindChild("button_ok");
        if (button_ok != null)
        {
            button_ok.GetComponent<Button>().onClick.AddListener(this.on_touch);
        }
    }


    public void refresh(string text, System.Action callback)
    {
        this.callback = callback;
        this.txt_msg.text = text;
    }


    void on_touch()
    {
        Debug.Log("on touch");
        this.callback();
    }
}
