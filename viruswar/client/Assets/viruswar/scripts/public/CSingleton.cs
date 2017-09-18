using UnityEngine;
using System.Collections;

public abstract class CSingletonMonobehaviour<T> : MonoBehaviour where T : MonoBehaviour
{
	static T instance;

	public static T Instance
	{
		get
		{
			if (null == instance)
			{
				instance = FindObjectOfType(typeof(T)) as T;
				if (null == instance)
				{
					//Debug.Log("Cannot find Manager Instance. Create a new one." + typeof(T).Name);
					GameObject obj = new GameObject(typeof(T).Name);
					instance = obj.AddComponent<T>();
				}
			}

			return instance;
		}
	}
}


public abstract class CSingleton<T> where T : class, new()
{
	static T instance;

	public static T Instance
	{
		get
		{
			if (instance == null)
			{
				instance = new T();
			}

			return instance;
		}
	}
}
