using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// World object that contains game objects.
/// </summary>
public class CGameWorld : CSingletonMonobehaviour<CGameWorld>
{
    /// <summary>
    /// Creates an object within the world.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public GameObject instantiate(GameObject obj)
    {
        // When creating an object, make it a child of CGameWorld.
        // This was done to make it easier to debug where objects are located.
        GameObject clone = GameObject.Instantiate(obj);
        clone.transform.parent = transform;
        return clone;
    }
}
