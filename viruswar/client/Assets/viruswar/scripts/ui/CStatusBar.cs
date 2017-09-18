using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FreeNet;
using GameServer;

public class CStatusBar : MonoBehaviour {

    Text txt_count;


    void Awake()
    {
        this.txt_count = transform.FindChild("msg_count").GetComponent<Text>();
    }


    void OnEnable()
    {
        StopAllCoroutines();
        StartCoroutine(routine_request_concurrent_users());
    }


    public void refresh(int count)
    {
        this.txt_count.text = string.Format("{0:n0}", count);
    }


    IEnumerator routine_request_concurrent_users()
    {
        WaitForSeconds sec = new WaitForSeconds(5);
        while (true)
        {
            send();
            yield return sec;
        }
    }


    void send()
    {
        if (!CNetworkManager.Instance.is_connected())
        {
            return;
        }

        CPacket msg = CPacket.create((short)PROTOCOL.CONCURRENT_USERS);
        CNetworkManager.Instance.send(msg);
    }
}
