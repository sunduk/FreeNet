using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using FreeNet;
using GameServer;

public class CPopupNetworkProcessing : MonoBehaviour
{
    Text txt_info;
    Text txt_progress;


    void Awake()
    {
        this.txt_info = transform.FindChild("info").GetComponent<Text>();
        this.txt_progress = transform.FindChild("progress").GetComponent<Text>();
        transform.FindChild("button_cancel").GetComponent<Button>().onClick.AddListener(this.on_cancel);
    }


    public void refresh(string text)
    {
        this.txt_info.text = text;

        StopAllCoroutines();
        StartCoroutine(routine_progress());
    }


    IEnumerator routine_progress()
    {
        WaitForSeconds delay = new WaitForSeconds(0.2f);
        while (true)
        {
            for (int i = 1; i <= 5; ++i)
            {
                this.txt_progress.text = repeat(".", i);
                yield return delay;
            }
        }
    }


    string repeat(string s, int n)
    {
        string result = string.Empty;
        for (int i = 0; i < n; ++i)
        {
            result += s;
        }

        return result;
    }


    void on_cancel()
    {
        CNetworkManager.Instance.disconnect();
    }


    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            on_cancel();
        }
    }
}
