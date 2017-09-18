using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CPlayerGameInfoUI : MonoBehaviour {

    Text txt_score;
    Image img_face;
    Image img_me;
    Image img_turn;


    void Awake()
    {
        this.txt_score = transform.FindChild("score").GetComponent<Text>();
        this.img_face = transform.FindChild("face").GetComponent<Image>();
        this.img_me = transform.FindChild("me").GetComponent<Image>();
        this.img_turn = transform.FindChild("turn").GetComponent<Image>();
    }


    public void refresh_me(bool flag)
    {
        this.img_me.gameObject.SetActive(flag);
    }


    public void refresh_score(int score)
    {
        this.txt_score.text = score.ToString();
    }


    public void refresh_turn(bool flag)
    {
        this.img_turn.gameObject.SetActive(flag);
    }
}
