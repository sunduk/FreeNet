using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;


public class CStateManager : MonoBehaviour
{
    public Enum current_state_type { get; private set; }
    IState current_state;
    Dictionary<Enum, IState> states = new Dictionary<Enum, IState>();
    IStateObjectGenerationType generation_type;
    Dictionary<IState, CMessageDispatcher> message_dispatcher;


    public void initialize(STATE_OBJECT_TYPE object_type)
    {
        this.message_dispatcher = new Dictionary<IState, CMessageDispatcher>();

        switch (object_type)
        {
            case STATE_OBJECT_TYPE.ATTACH_TO_SINGLE_OBJECT:
                this.generation_type = new CStateSingleObject();
                break;

            case STATE_OBJECT_TYPE.CREATE_NEW_OBJECT:
                this.generation_type = new CStateCreateNewObject();
                break;
        }
    }


    public void register_message_handler(IState state, Enum key, MessageHandlerDelegate fn)
    {
        if (!this.message_dispatcher.ContainsKey(state))
        {
            this.message_dispatcher.Add(state, new CMessageDispatcher());
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


    public void add<T>(Enum key) where T : MonoBehaviour, IState
    {
        add(key, this.generation_type.make_state_object<T>(gameObject, key));
    }


    void add(Enum key, IState state)
    {
        this.generation_type.set_active(state, false);

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
            this.generation_type.set_active(this.current_state, false);
        }

        Debug.Log(string.Format("[StateManager] change state {0} -> {1}", this.current_state_type, next_state));

        this.current_state_type = next_state;
        this.current_state = this.states[next_state];
        this.generation_type.set_active(this.current_state, true);
        this.current_state.on_enter();
    }


    public void send_state_message(System.Enum message, params object[] args)
    {
        if (this.current_state == null)
        {
            return;
        }

        if (!this.message_dispatcher.ContainsKey(this.current_state))
        {
            return;
        }

        this.message_dispatcher[this.current_state].dispatch(message, args);
    }


    public bool is_current_state(System.Enum state)
    {
        return Enum.Equals(this.current_state_type, state);
    }
}
