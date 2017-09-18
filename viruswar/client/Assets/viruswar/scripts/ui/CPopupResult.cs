using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using FreeNet;
using GameServer;

public class CPopupResult : MonoBehaviour
{
    Image img_face_01;
    Image img_face_02;
    Text txt_result;


    void Awake()
    {
        this.txt_result = transform.FindChild("result").GetComponent<Text>();
        this.img_face_01 = transform.FindChild("face_01").GetComponent<Image>();
        this.img_face_02 = transform.FindChild("face_02").GetComponent<Image>();

        transform.FindChild("bg").GetComponent<Button>().onClick.AddListener(this.on_ok);
        transform.FindChild("button_ok").GetComponent<Button>().onClick.AddListener(this.on_ok);
    }


    public void on_ok()
    {
        CUIManager.Instance.get_uipage(UI_PAGE.PLAY_ROOM).GetComponent<CBattleRoom>().back_to_main();
    }


    public void refresh(byte win_player_index, byte player_me_index)
    {
        if (win_player_index == byte.MaxValue)
        {
            // draw.
            this.txt_result.text = "무승부";
        }
        else
        {
            bool win = win_player_index == player_me_index;
            if (win)
            {
                this.txt_result.text = "승리!!";
            }
            else
            {
                this.txt_result.text = "패배...";
            }
        }

        if (player_me_index == 0)
        {
            this.img_face_01.gameObject.SetActive(true);
            this.img_face_02.gameObject.SetActive(false);
        }
        else
        {
            this.img_face_01.gameObject.SetActive(false);
            this.img_face_02.gameObject.SetActive(true);
        }
    }
}
