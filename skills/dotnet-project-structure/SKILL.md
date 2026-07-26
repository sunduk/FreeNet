---
name: dotnet-project-structure
description: Use when adding, splitting, or wiring .NET projects/solutions in this repository.
license: MIT-inspired adaptation from Aaronontheweb/dotnet-skills
---

# .NET Project Structure (FreeNet)

This repo has two solution entry points:
- `FreeNet.slnx` (library + sample client/server + VirusWar server project)
- `viruswar/server/viruswar_server.slnx` (VirusWar server focused)

## Structure constraints

- `FreeNet/` is the reusable networking library (`net10.0`).
- `CSampleServer/` and `CSampleClient/` are executable integration samples (`net10.0`) referencing `FreeNet`.
- `viruswar/server/GameServer/` references `FreeNet` and is included in both architectural flows.

## Change patterns

1. **Adding a new executable sample**
   - Use `net10.0`, add `ProjectReference` to `..\FreeNet\FreeNet.csproj`, include in `FreeNet.slnx`.
2. **Adding core library APIs**
   - Keep in `FreeNet/` and avoid coupling to sample app directories.
3. **Adding protocol features**
   - Align enum/message handling on both sender and receiver projects.

## Validation baseline

- Build `.\FreeNet.slnx` for broad compatibility.
- Build `.\viruswar\server\viruswar_server.slnx` when touching VirusWar paths.
