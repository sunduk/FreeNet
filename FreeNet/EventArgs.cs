namespace FreeNet;

/// <summary>
/// Represents event data of a specified type. Implements the <see cref="EventArgs"/>
/// </summary>
/// <typeparam name="T">The type of the event data.</typeparam>
/// <seealso cref="EventArgs"/>
/// <remarks>Initializes a new instance of the <see cref="EventArgs{T}"/> class.</remarks>
/// <param name="value">The value.</param>
public class EventArgs<T>(T? value) : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventArgs{T}"/> class.
    /// </summary>
    public EventArgs() : this(default)
    {
    }

    /// <summary>
    /// Gets or sets the value.
    /// </summary>
    /// <value>The value.</value>
    public T? Value { get; set; } = value;
}
