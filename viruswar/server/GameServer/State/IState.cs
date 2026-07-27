namespace GameServer.State;

/// <summary>
/// Interface for state.
/// </summary>
public interface IState
{
    /// <summary>
    /// Called when [enter].
    /// </summary>
    void OnEnter();

    /// <summary>
    /// Called when [exit].
    /// </summary>
    void OnExit();
}