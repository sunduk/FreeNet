namespace GameServer.State;

/// <summary>
/// State manager.
/// Handles transitions between states and message dispatch to state objects.
/// T and T2 are parameter types passed with dispatched messages.
/// TODO: Currently fixed to two parameter types; consider a more flexible design.
/// </summary>
/// <typeparam name="T">Type of the first message parameter.</typeparam>
/// <typeparam name="T2">Type of the second message parameter.</typeparam>
public class StateManager<T, T2>
{
    /// <summary>
    /// Message dispatch table by state.
    /// </summary>
    private readonly Dictionary<IState, MessageDispatcher<T, T2>> _messageDispatcher;

    private readonly Dictionary<Enum, IState> _states = [];
    private IState? _currentState;

    /// <summary>
    /// Current state type.
    /// </summary>
    private Enum? _currentStateType;

    public StateManager() => _messageDispatcher = [];

    public void Add(Enum key, IState state)
    {
        if (!_states.TryAdd(key, state))
        {
            _states[key] = state;
        }
    }

    public void ChangeState(Enum next_state)
    {
        _currentState?.OnExit();

        _currentStateType = next_state;
        _currentState = _states[next_state];
        _currentState.OnEnter();
    }

    public bool IsCurrentState(System.Enum state) => Enum.Equals(_currentStateType, state);

    public void RegisterMessageHandler(IState state, Enum key, MessageHandlerDelegate<T, T2> fn)
    {
        if (!_messageDispatcher.TryGetValue(state, out var value))
        {
            value = new MessageDispatcher<T, T2>();
            _messageDispatcher.Add(state, value);
        }

        value.Register(key, fn);
    }

    /// <summary>
    /// Sends a message to the currently active state object.
    /// </summary>
    /// <param name="message">Message to send.</param>
    /// <param name="t1">First parameter sent with the message.</param>
    /// <param name="t2">Second parameter sent with the message.</param>
    public void SendStateMessage(System.Enum message, T t1, T2 t2)
    {
        if (_currentState is null)
        {
            return;
        }

        if (_messageDispatcher.TryGetValue(_currentState, out var value))
        {
            value.Dispatch(message, t1, t2);
        }
    }

    public void UnregisterMessageHandler(IState state, Enum key)
    {
        if (_messageDispatcher.TryGetValue(state, out var value))
        {
            value.Unregister(key);
        }
    }
}
