using FreeNet;

namespace GameServer;

public class Player(GameUser user, byte playerIndex)
{
    private readonly GameUser _owner = user;

    public delegate void SendFn(Packet message);

    public byte PlayerIndex { get; private set; } = playerIndex;
    public List<short> Viruses { get; private set; } = [];

    public void AddCell(short position) => Viruses.Add(position);

    public void Disconnect() => _owner.Disconnect();

    public int GetVirusCount() => Viruses.Count;

    public void RemoveCell(short position) => Viruses.Remove(position);

    public void Removed() => _owner.ChangeState(UserState.UserStateType.Lobby);

    public void Reset() => Viruses.Clear();

    public void Send(Packet message) => _owner.Send(message);
}
