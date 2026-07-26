---
name: acquire-codebase-knowledge
description: Use when mapping or documenting this repository at architecture level. Trigger for onboarding, architecture docs, or "understand this repo" requests.
license: MIT-inspired adaptation from github/awesome-copilot
---

# Acquire Codebase Knowledge (FreeNet edition)

Produce concise, evidence-backed documentation for this repository without guessing.

## Required outputs

Create/update these documents in `docs/codebase/`:
1. `STACK.md`
2. `STRUCTURE.md`
3. `ARCHITECTURE.md`
4. `CONVENTIONS.md`
5. `TESTING.md`
6. `CONCERNS.md`

Every non-trivial claim must cite file paths.

## Investigation sequence

1. Read `README.md`, `TestManual.md`, `.github/copilot-instructions.md`, and `AGENTS.md`.
2. Map solutions and projects:
   - `FreeNet.slnx`
   - `viruswar/server/viruswar_server.slnx`
   - `*.csproj` under `FreeNet/`, `CSampleServer/`, `CSampleClient/`, `viruswar/server/GameServer/`
3. Map runtime architecture from:
   - `FreeNet/CNetworkService.cs`
   - `FreeNet/CUserToken.cs`
   - `FreeNet/CMessageResolver.cs`
   - `FreeNet/CLogicMessageEntry.cs`
   - `FreeNet/CListener.cs`
4. Capture protocol/threading conventions from:
   - `FreeNet/CPacket.cs`
   - `FreeNet/IPeer.cs`
   - sample/game `protocol.cs` files
5. Document testing reality from available manual test flow and runnable sample apps.

## FreeNet-specific checks

- Confirm packet framing assumptions (`Defines.HEADERSIZE`, `record_size()` usage).
- Confirm thread mode semantics (`new CNetworkService(true|false)`).
- Confirm protocol ID reservation (`<= 0` system flow).
- Confirm any client/server protocol divergence.

## Guardrails

- Do not invent CI/test/lint pipelines.
- Mark unknowns as `[TODO]`.
- Use `[ASK USER]` only for intent decisions that code cannot answer.
