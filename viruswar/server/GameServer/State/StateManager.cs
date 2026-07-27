namespace GameServer.State;

/// <summary>
/// 상태 매니저. 각 상태들의 전환과 상태 객체에 메지시 전송 기능을 담당한다. T, T2는 메시지 전송시 보낼 파라미터 타입이다. todo:두개로 고정되어 있는데 좀 더 유연하게 바꾸는 방법을 고민중이다.
/// </summary>
/// <typeparam name="T">메시지 전송시 보낼 파라미터 타입이다.</typeparam>
/// <typeparam name="T2">메시지 전송시 보낼 두 번째 파라미터 타입이다.</typeparam>
public class StateManager<T, T2>
{
    /// <summary>
    // 메시지 관리.
    /// </summary>
    private readonly Dictionary<IState, MessageDispatcher<T, T2>> _messageDispatcher;

    private readonly Dictionary<Enum, IState> _states = [];
    private IState _currentState;

    /// <summary>
    // 상태 관리.
    /// </summary>
    private Enum _currentStateType;

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
    /// 현재 수행중인 상태 객체에 메시지를 전송한다.
    /// </summary>
    /// <param name="message">전송할 메시지</param>
    /// <param name="t1">메시지와 함께 전송할 첫 번째 파라미터</param>
    /// <param name="t2">메시지와 함께 전송할 두 번째 파라미터</param>
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
