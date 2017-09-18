using System;
using System.Collections;
using System.Collections.Generic;


/// <summary>
/// 상태 매니저.
/// 각 상태들의 전환과 상태 객체에 메지시 전송 기능을 담당한다.
/// 
/// T, T2는 메시지 전송시 보낼 파라미터 타입이다.
/// todo:두개로 고정되어 있는데 좀 더 유연하게 바꾸는 방법을 고민중이다.
/// </summary>
/// <typeparam name="T"></typeparam>
/// <typeparam name="T2"></typeparam>
public class CStateManager<T, T2>
{
    //----------------------------------------------
    // 상태 관리.
    //----------------------------------------------
    Enum current_state_type;
    IState current_state;
    Dictionary<Enum, IState> states = new Dictionary<Enum, IState>();


    //----------------------------------------------
    // 메시지 관리.
    //----------------------------------------------
    Dictionary<IState, CMessageDispatcher<T, T2>> message_dispatcher;


    public CStateManager()
    {
        this.message_dispatcher = new Dictionary<IState, CMessageDispatcher<T, T2>>();
    }


    public void register_message_handler(IState state, Enum key, MessageHandlerDelegate<T, T2> fn)
    {
        if (!this.message_dispatcher.ContainsKey(state))
        {
            this.message_dispatcher.Add(state, new CMessageDispatcher<T, T2>());
        }

        this.message_dispatcher[state].register(key, fn);
    }


    public void unregister_message_handler(IState state, Enum key)
    {
        if (!this.message_dispatcher.ContainsKey(state))
        {
            return;
        }

        this.message_dispatcher[state].unregister(key);
    }


    public void add(Enum key, IState state)
    {
        if (!this.states.ContainsKey(key))
        {
            this.states.Add(key, state);
            return;
        }

        this.states[key] = state;
    }


    public void change_state(Enum next_state)
    {
        if (this.current_state != null)
        {
            this.current_state.on_exit();
        }

        this.current_state_type = next_state;
        this.current_state = this.states[next_state];
        this.current_state.on_enter();
    }


    /// <summary>
    /// 현재 수행중인 상태 객체에 메시지를 전송한다.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="t1"></param>
    /// <param name="t2"></param>
    public void send_state_message(System.Enum message, T t1, T2 t2)
    {
        if (this.current_state == null)
        {
            return;
        }

        if (!this.message_dispatcher.ContainsKey(this.current_state))
        {
            return;
        }

        this.message_dispatcher[this.current_state].dispatch(message, t1, t2);
    }


    public bool is_current_state(System.Enum state)
    {
        return Enum.Equals(this.current_state_type, state);
    }
}
