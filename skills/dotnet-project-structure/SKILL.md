---
name: dotnet-project-structure
description: Use when adding, splitting, or wiring .NET projects/solutions in this repository.
license: MIT-inspired adaptation from Aaronontheweb/dotnet-skills
---

# .NET Project Structure (FreeNet)

This repo has two solution entry points:
- `FreeNet.sln` (library + sample client/server + VirusWar server project)
- `viruswar/server/viruswar_server.sln` (VirusWar server focused)

## Structure constraints

- `FreeNet/` is the reusable networking library (`netstandard2.0`).
- `CSampleServer/` and `CSampleClient/` are executable integration samples (`net8.0`) referencing `FreeNet`.
- `viruswar/server/GameServer/` references `FreeNet` and is included in both architectural flows.

## Change patterns

1. **Adding a new executable sample**
   - Use `net8.0`, add `ProjectReference` to `..\FreeNet\FreeNet.csproj`, include in `FreeNet.sln`.
2. **Adding core library APIs**
   - Keep in `FreeNet/` and avoid coupling to sample app directories.
3. **Adding protocol features**
   - Align enum/message handling on both sender and receiver projects.

## Validation baseline

- Build `.\FreeNet.sln` for broad compatibility.
- Build `.\viruswar\server\viruswar_server.sln` when touching VirusWar paths.
