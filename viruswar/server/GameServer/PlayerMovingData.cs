namespace GameServer;

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

internal class PlayerMovingData
{
    public Dictionary<MOVE_DIRECTION, float> Accelerations = [];

    public byte PlayerIndex;

    public float PositionX;

    public float PositionY;

    public float PositionZ;

    public PlayerMovingData(byte playerIndex, float x, float y, float z)
    {
        PlayerIndex = playerIndex;
        PositionX = x;
        PositionY = y;
        PositionZ = z;

        foreach (var e in Enum.GetValues<MOVE_DIRECTION>())
        {
            Accelerations.Add(e, 0.0f);
        }
    }
}
