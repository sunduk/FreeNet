namespace GameServer;

public struct Vector2(float x, float y)
{
    public float X = x;
    public float Y = y;

    public static Vector2 operator -(Vector2 a, Vector2 b) => new(a.X - b.X, a.Y - b.Y);
}
