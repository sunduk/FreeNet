namespace GameServer.State;

public delegate void MessageHandlerDelegate<T, T2>(T t1, T2 t2);

public class MessageDispatcher<T, T2>
{
    private readonly Dictionary<Enum, MessageHandlerDelegate<T, T2>> _handlers;

    public MessageDispatcher() => _handlers = [];

    public void Dispatch(Enum key, T t1, T2 t2)
    {
        if (_handlers.TryGetValue(key, out var value))
        {
            value(t1, t2);
        }
    }

    public void Register(Enum key, MessageHandlerDelegate<T, T2> fn)
    {
        if (!_handlers.TryAdd(key, fn))
        {
            _handlers[key] = fn;
        }
    }

    public void Unregister(Enum key) => _handlers.Remove(key);
}
