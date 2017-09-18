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
        public delegate void SendFn(CPacket msg);

        IPeer owner;

        public byte player_index { get; private set; }
        public List<short> viruses { get; private set; }

        public CPlayer(CGameUser user, byte player_index)
        {
            this.owner = user;
            this.player_index = player_index;
            this.viruses = new List<short>();
        }

        public void reset()
        {
            this.viruses.Clear();
        }

        public void add_cell(short position)
        {
            this.viruses.Add(position);
        }

        public void remove_cell(short position)
        {
            this.viruses.Remove(position);
        }

        public void send(CPacket msg)
        {
            this.owner.send(msg);
        }

        public int get_virus_count()
        {
            return this.viruses.Count;
        }

        public void removed()
        {
            ((CGameUser)this.owner).change_state(UserState.USER_STATE_TYPE.LOBBY);
        }

        public void disconnect()
        {
            this.owner.disconnect();
        }
    }
}
