using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public enum UI_PAGE
{
    MAIN_MENU,
	PLAY_ROOM,
    POPUP_NETWORK_PROCESSING,
    GAME_RESULT,
    POPUP_COMMON,
    POPUP_WAIT,
    POPUP_QUIT,
    STATUS_BAR,
}

public class CUIManager : CSingletonMonobehaviour<CUIManager>
{
	Dictionary<UI_PAGE, GameObject> ui_objects;

	void Awake()
	{
		this.ui_objects = new Dictionary<UI_PAGE, GameObject>();
		this.ui_objects.Add(UI_PAGE.MAIN_MENU, transform.FindChild("main_menu").gameObject);
        this.ui_objects.Add(UI_PAGE.PLAY_ROOM, transform.FindChild("play_room").gameObject);
        this.ui_objects.Add(UI_PAGE.POPUP_NETWORK_PROCESSING, transform.FindChild("popup_matching").gameObject);
        this.ui_objects.Add(UI_PAGE.GAME_RESULT, transform.FindChild("popup_result").gameObject);
        this.ui_objects.Add(UI_PAGE.POPUP_COMMON, transform.FindChild("popup_common").gameObject);
        this.ui_objects.Add(UI_PAGE.POPUP_WAIT, transform.FindChild("popup_wait").gameObject);
        this.ui_objects.Add(UI_PAGE.POPUP_QUIT, transform.FindChild("popup_quit").gameObject);
        this.ui_objects.Add(UI_PAGE.STATUS_BAR, transform.FindChild("status_bar").gameObject);

        hide_all();
	}


    void Start()
    {
        show(UI_PAGE.MAIN_MENU);
        get_uipage(UI_PAGE.MAIN_MENU).GetComponent<CMainMenu>().enter();
    }


	public GameObject get_uipage(UI_PAGE page)
	{
		return this.ui_objects[page];
	}


	public void show(UI_PAGE page)
	{
		this.ui_objects[page].SetActive(true);
	}


	public void hide(UI_PAGE page)
	{
		this.ui_objects[page].SetActive(false);
	}


    public void hide_all()
    {
        foreach (KeyValuePair<UI_PAGE, GameObject> kvp in this.ui_objects)
        {
            kvp.Value.SetActive(false);
        }
    }
}
