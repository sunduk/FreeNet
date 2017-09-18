using UnityEngine;
using System;
using System.Collections;

public class CStateCreateNewObject : IStateObjectGenerationType
{
    IState IStateObjectGenerationType.make_state_object<T>(GameObject parent, Enum key)
    {
        GameObject obj = new GameObject(key.ToString());
        obj.transform.parent = parent.transform;
        return obj.AddComponent<T>();
    }


    void IStateObjectGenerationType.set_active(IState obj, bool flag)
    {
        ((MonoBehaviour)obj).gameObject.SetActive(flag);
    }
}
