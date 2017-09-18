using System;
using System.Collections.Generic;

public delegate void MessageHandlerDelegate<T, T2>(T t1, T2 t2);
public class CMessageDispatcher<T, T2>
{
    Dictionary<Enum, MessageHandlerDelegate<T, T2>> handlers;

    public CMessageDispatcher()
    {
        this.handlers = new Dictionary<Enum, MessageHandlerDelegate<T, T2>>();
    }


    public void register(Enum key, MessageHandlerDelegate<T, T2> fn)
    {
        if (!this.handlers.ContainsKey(key))
        {
            this.handlers.Add(key, fn);
            return;
        }

        this.handlers[key] = fn;
    }


    public void unregister(Enum key)
    {
        this.handlers.Remove(key);
    }


    public void dispatch(Enum key, T t1, T2 t2)
    {
        if (!this.handlers.ContainsKey(key))
        {
            return;
        }

        this.handlers[key](t1, t2);
    }
}
