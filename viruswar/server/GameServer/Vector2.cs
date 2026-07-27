namespace GameServer;

public struct Vector2(float x, float y)
{
    public float x = x;
    public float y = y;

    public static Vector2 operator -(Vector2 a, Vector2 b) => new(a.x - b.x, a.y - b.y);
}
