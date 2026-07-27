namespace FreeNet;

/// <summary>
/// A record struct that represents a constant value of type <typeparamref name="T"/>.
/// </summary>
public readonly record struct Const<T>(T Value);