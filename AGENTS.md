# Agent instructions (repository-wide)

This repository uses a small async TCP networking core (`FreeNet/`) with sample app surfaces (`CSampleServer/`, `CSampleClient/`, `viruswar/server/GameServer/`).

## Execute-first command set

- Restore/build main solution:
  - `dotnet restore .\FreeNet.sln`
  - `dotnet build .\FreeNet.sln -c Debug`
- Build VirusWar server solution:
  - `dotnet build .\viruswar\server\viruswar_server.sln -c Debug`
- Run sample server/client:
  - `dotnet run --project .\CSampleServer\CSampleServer.csproj`
  - `dotnet run --project .\CSampleClient\CSampleClient.csproj`

## Architectural invariants

- Packet framing is fixed-length-header-first (`Defines.HEADERSIZE = 4`), then protocol/body parsing through `CPacket` and `CMessageResolver`.
- `CNetworkService` owns listener startup, SAEA pooling, session setup, and heartbeat lifecycle.
- `CUserToken` is the authoritative per-connection state object; do not bypass it for send/receive/session closure.
- `IPeer` implementations are the app boundary and must attach via `token.set_peer(this)`.
- Dispatch mode is a deliberate switch:
  - `CNetworkService(false)`: dispatch on IO completion threads.
  - `CNetworkService(true)`: queue packets and dispatch on single logic thread (`CLogicMessageEntry`).

## Protocol and threading conventions

- Reserve protocol IDs `<= 0` for system behavior (close/heartbeat).
- Always call `record_size()` before sending packets unless using a helper that does so immediately before transport.
- Parse payload fields in exactly the same order they were pushed.
- `session_created_callback` may be concurrent; lock shared collections in callback flows.

## Existing test reality

- No automated test project exists right now.
- Use sample server/client for smoke checks and `README.md` / `TestManual.md` for manual/load testing flow.

## Source of truth

- For Copilot-specific behavior, also follow `.github/copilot-instructions.md`.
- Reusable local skills are in `skills/` (see `skills/README.md`).
- For workflows with multiple tool paths (CLI/MCP/etc.), prefer whichever path minimizes token usage while preserving correctness.
