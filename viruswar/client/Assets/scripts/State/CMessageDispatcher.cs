using System;
using System.Collections.Generic;

public delegate void MessageHandlerDelegate(params object[] args);
public class CMessageDispatcher
{
    Dictionary<Enum, MessageHandlerDelegate> handlers;

    public CMessageDispatcher()
    {
        this.handlers = new Dictionary<Enum, MessageHandlerDelegate>();
    }


    public void register(Enum key, MessageHandlerDelegate fn)
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


    public void dispatch(Enum key, params object[] args)
    {
        if (!this.handlers.ContainsKey(key))
        {
            return;
        }

        this.handlers[key](args);
    }
}
