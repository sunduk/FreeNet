using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    public enum MOVE_DIRECTION : byte
    {
        NONE,
        UP,
        DOWN,
        LEFT,
        RIGHT,
        UP_LEFT,
        UP_RIGHT,
        DOWN_LEFT,
        DOWN_RIGHT
    }

    class CPlayerMovingData
    {
        public CPlayerMovingData(byte player_index, float x, float y, float z)
        {
            this.player_index = player_index;
            this.position_x = x;
            this.position_y = y;
            this.position_z = z;

            foreach (MOVE_DIRECTION e in Enum.GetValues(typeof(MOVE_DIRECTION)))
            {
                this.accelerations.Add(e, 0.0f);
            }
        }


        public byte is_changed;

        public byte player_index;

        public float position_x;
        public float position_y;
        public float position_z;

        public Dictionary<MOVE_DIRECTION, float> accelerations = new Dictionary<MOVE_DIRECTION, float>();

        public byte direction;
    }
}
