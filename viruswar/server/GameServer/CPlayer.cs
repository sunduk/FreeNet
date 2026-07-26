using System;
using System.Collections;
using System.Collections.Generic;
using FreeNet;

public enum PLAYER_TYPE : byte
{
    HUMAN,
    AI
}

namespace GameServer
{
    public class CPlayer
    {
        public delegate void SendFn(Packet msg);

        IPeer owner;

        public byte player_index { get; private set; }
        public List<short> viruses { get; private set; }

        public CPlayer(CGameUser user, byte player_index)
        {
            owner = user;
            this.player_index = player_index;
            viruses = new List<short>();
        }

        public void reset()
        {
            viruses.Clear();
        }

        public void add_cell(short position)
        {
            viruses.Add(position);
        }

        public void remove_cell(short position)
        {
            viruses.Remove(position);
        }

        public void send(Packet msg)
        {
            owner.Send(msg);
        }

        public int get_virus_count()
        {
            return viruses.Count;
        }

        public void removed()
        {
            ((CGameUser)owner).change_state(UserState.USER_STATE_TYPE.LOBBY);
        }

        public void disconnect()
        {
            owner.Disconnect();
        }
    }
}
