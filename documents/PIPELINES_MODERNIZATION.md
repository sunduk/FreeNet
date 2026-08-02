# System.IO.Pipelines Modernization (v0.2.0)

## Overview

FreeNet v0.2.0 modernized transport I/O around `System.IO.Pipelines`. The core send/receive flow now runs on asynchronous pipeline loops while preserving the existing packet framing model (`HEADERSIZE = 4`) and application protocol handling.

## Implemented Changes in v0.2.0

### 1. Pipeline-driven connection startup

`NetworkService` now starts per-connection pipeline loops when a socket connects:

- `UserToken.StartPipelinesAsync(CancellationToken cancellationToken = default)`
- Called for both accepted server connections and connected client sockets

### 2. Send path moved to a `Pipe`

`UserToken.Send(...)` writes outbound bytes into an internal `PipeWriter` (`_sendPipe.Writer`). A background send loop drains the pipe and performs `Socket.SendAsync(...)` calls.

### 3. Dedicated async receive/send loops

`UserToken` runs two tasks:

- `ReceiveLoopAsync(...)`: receives socket data and forwards bytes to `MessageResolver`
- `SendLoopAsync(...)`: flushes buffered outbound segments from the pipe to the socket

### 4. Graceful shutdown semantics

`Disconnect()` completes the pipe writer so pending sends can drain before TCP half-close (`SocketShutdown.Send`), while `Close()` still handles immediate teardown and session cleanup.

## Backward Compatibility Notes

- Message parsing and dispatch contracts are unchanged (`Packet`, `MessageResolver`, `IPeer`).
- Protocol IDs for system behavior remain reserved (`<= 0`).
- Existing app-level packet handlers continue to work without protocol changes.

## Related repository updates

The v0.2.0 repository updates also include:

- Version metadata updated to `0.2.0` (`VersionPrefix`, `AssemblyVersion`, `FileVersion`)
- `LICENSE` file added with the MIT license
- `README.md` version and license sections updated

## References

- [README - Version](../README.md#version)
- [README - Structure](../README.md#structure)
- [Microsoft Learn: System.IO.Pipelines](https://learn.microsoft.com/en-us/dotnet/standard/io/pipelines)
