using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CButtonAction : MonoBehaviour {

    public int index { get; private set; }


    public void set(int index)
    {
        this.index = index;
    }
}
