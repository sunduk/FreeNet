# Copilot instructions for FreeNet

## Build, test, and lint

### Build
- Restore/build the main solution:
  - `dotnet restore .\FreeNet.slnx`
  - `dotnet build .\FreeNet.slnx -c Debug`
- Build the VirusWar server solution:
  - `dotnet build .\viruswar\server\viruswar_server.slnx -c Debug`

### Run samples
- Start sample server:
  - `dotnet run --project .\CSampleServer\CSampleServer.csproj`
- Start sample client:
  - `dotnet run --project .\CSampleClient\CSampleClient.csproj`

### Tests
- Run all tests:
  - `dotnet test .\FreeNet.slnx -c Debug`
- Run VirusWar server tests:
  - `dotnet test .\viruswar\server\viruswar_server.slnx -c Debug`
- Run a single test project:
  - `dotnet test .\FreeNet.Tests\FreeNet.Tests.csproj -c Debug`
- Test runner is Microsoft Testing Platform (MTP) via `global.json`.
- Manual/load validation flow is still documented in `README.md` and `TestManual.md` for socket stress scenarios.

### Lint/format
- No dedicated lint configuration is present in the repository (no `.editorconfig`, no lint scripts, no analyzer config files).

## High-level architecture

- `FreeNet/` is the core networking library (TCP, async receive/send, pooling, packet framing).
- `CNetworkService` is the main composition root:
  - owns `SocketAsyncEventArgs` pools and shared receive buffers,
  - starts `CListener`,
  - creates `CUserToken` sessions per connection,
  - wires session lifecycle callbacks and heartbeat checks.
- `CUserToken` is the per-connection transport/session object:
  - receives raw bytes and delegates framing to `CMessageResolver`,
  - manages outbound queue batching via `SocketAsyncEventArgs.BufferList`,
  - handles system protocols for close/heartbeat.
- `CMessageResolver` reconstructs complete packets from stream fragments using a fixed 4-byte length header (`Defines.HEADERSIZE = 4`).
- Message dispatch has two modes selected by `CNetworkService(use_logicthread)`:
  - `false`: packet handling runs on IO completion threads.
  - `true`: packets are queued through `CLogicMessageEntry` + `CDoubleBufferingQueue` and processed on one logic thread.
- `IPeer` is the application-facing session contract; sample/game servers implement it to process protocol messages and cleanup on disconnect.
- App/sample surfaces:
  - `CSampleServer/` and `CSampleClient/` are minimal integration examples.
  - `viruswar/server/GameServer/` is a fuller game-server usage of the library.
- `documents/*.png` diagrams in README are part of the intended architecture documentation and should stay consistent with networking/dispatch behavior.

## Key conventions in this codebase

- Naming convention is legacy C-style:
  - classes typically start with `C` (for example `CUserToken`, `CNetworkService`),
  - interfaces start with `I`,
  - many methods use lower-case snake/camel hybrids (`on_message`, `on_removed`, `session_created_callback`).
- Packet protocol contract is strict:
  - create with `CPacket.create(protocolId)`,
  - `push(...)` payload fields in order,
  - call `record_size()` before sending (or rely on helpers that do it immediately before send),
  - parse with `pop_*` in exactly the same order.
- Protocol IDs `<= 0` are reserved for system-level behavior (`SYS_CLOSE_REQ`, `SYS_CLOSE_ACK`, heartbeat control) and should not be reused by game/application protocols.
- `IPeer` implementations are expected to bind themselves in constructors via `token.set_peer(this)`.
- `session_created_callback` can be invoked concurrently from IO paths; shared state mutations (for example user lists) are expected to be locked.
- For non-FreeNet test clients, heartbeat may need to be disabled in sample server (`service.disable_heartbeat()`), matching repository README guidance.
- For quick transport validation, sample server includes an optional echo path in `CSampleServer/CGameUser.cs` (commented toggle).
- Keep protocol enums and parser usage aligned across client/server projects (`CSampleServer/protocol.cs`, `CSampleClient/protocol.cs`, `viruswar/server/GameServer/protocol.cs`).
- NuGet package versions are centrally managed via `Directory.Packages.props`; add/update versions there rather than per-project.

## Pull request workflow

- When creating PRs, assign them to the current user/requester.
- Add the most appropriate labels for the change.
- Set a milestone when one applies.
