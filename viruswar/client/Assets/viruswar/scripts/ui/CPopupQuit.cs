using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using FreeNet;
using GameServer;

public class CPopupQuit : MonoBehaviour
{
    System.Action callback = delegate { };


    void Awake()
    {
        transform.FindChild("button_ok").GetComponent<Button>().onClick.AddListener(this.on_ok);
        transform.FindChild("button_cancel").GetComponent<Button>().onClick.AddListener(this.on_cancel);
    }


    public void refresh(System.Action callback)
    {
        this.callback = callback;
    }


    void on_ok()
    {
        this.callback();
    }


    void on_cancel()
    {
        CUIManager.Instance.hide(UI_PAGE.POPUP_QUIT);
    }


    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            on_cancel();
        }
    }
}
