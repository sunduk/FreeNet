using UnityEngine;
using System;
using System.Collections;

public class CStateSingleObject : IStateObjectGenerationType
{
    IState IStateObjectGenerationType.make_state_object<T>(GameObject parent, Enum key)
    {
        return parent.AddComponent<T>();
    }


    void IStateObjectGenerationType.set_active(IState obj, bool flag)
    {
        ((MonoBehaviour)obj).StopAllCoroutines();
        ((MonoBehaviour)obj).enabled = flag;
    }
}
