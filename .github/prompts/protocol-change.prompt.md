# Protocol-safe change prompt

Make the requested protocol/networking change in this repository while preserving wire compatibility and thread-safety guarantees.

Constraints to respect:
- Packet framing uses a 4-byte size header (`Defines.HEADERSIZE = 4`).
- Application protocol IDs must remain `> 0`; values `<= 0` are reserved for system flow (close/heartbeat).
- `CPacket.push(...)` and `CPacket.pop_*()` order must stay exactly aligned.
- `IPeer` implementations must continue to bind with `token.set_peer(this)`.
- Validate both dispatch modes where relevant:
  - IO-thread path (`new CNetworkService(false)`)
  - Logic-thread path (`new CNetworkService(true)`)

When touching protocol definitions, update matching enums/handlers in:
- `CSampleServer\protocol.cs`
- `CSampleClient\protocol.cs`
- `viruswar\server\GameServer\protocol.cs` (if applicable to shared behavior)
