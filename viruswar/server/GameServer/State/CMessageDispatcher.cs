using System;
using System.Collections.Generic;

public delegate void MessageHandlerDelegate<T, T2>(T t1, T2 t2);
public class CMessageDispatcher<T, T2>
{
    Dictionary<Enum, MessageHandlerDelegate<T, T2>> handlers;

    public CMessageDispatcher()
    {
        handlers = new Dictionary<Enum, MessageHandlerDelegate<T, T2>>();
    }


    public void register(Enum key, MessageHandlerDelegate<T, T2> fn)
    {
        if (!handlers.ContainsKey(key))
        {
            handlers.Add(key, fn);
            return;
        }

        handlers[key] = fn;
    }


    public void unregister(Enum key)
    {
        handlers.Remove(key);
    }


    public void dispatch(Enum key, T t1, T2 t2)
    {
        if (!handlers.ContainsKey(key))
        {
            return;
        }

        handlers[key](t1, t2);
    }
}
