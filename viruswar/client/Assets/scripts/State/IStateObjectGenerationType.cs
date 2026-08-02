using UnityEngine;
using System;
using System.Collections;

public enum STATE_OBJECT_TYPE
{
    // Type that attaches all state scripts to the same object.
    ATTACH_TO_SINGLE_OBJECT,

    // Type that creates new game objects and attaches them as children.
    CREATE_NEW_OBJECT
}


/// <summary>
/// Classification based on state generation method.
/// </summary>
public interface IStateObjectGenerationType
{
    IState make_state_object<T>(GameObject parent, Enum key) where T : Component, IState;
    void set_active(IState obj, bool flag);
}
