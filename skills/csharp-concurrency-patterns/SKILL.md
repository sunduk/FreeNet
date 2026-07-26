---
name: csharp-concurrency-patterns
description: Use when changing threading, dispatch, receive/send flow, session lifecycle, or shared state handling in FreeNet.
license: MIT-inspired adaptation from Aaronontheweb/dotnet-skills
---

# C# Concurrency Patterns for FreeNet

Use this when code touches:
- `CNetworkService`
- `CUserToken`
- `CLogicMessageEntry`
- `CListener`
- shared collections in sample/game server code

## Decision guide

1. **Need deterministic single-threaded message handling?**
   - Keep or switch to `new CNetworkService(true)` and route through logic queue.
2. **Need highest throughput with IO-thread handlers?**
   - Keep `new CNetworkService(false)` and make shared-state synchronization explicit.
3. **Touching user/session collections?**
   - Preserve locking around shared `List<T>` and callback paths.
4. **Touching send queue behavior?**
   - Preserve `sending_list` lock semantics and BufferList batching assumptions.

## Rules to preserve

- Never mix assumptions between logic-thread and IO-thread modes.
- Keep `session_created_callback` safe for concurrent invocation.
- Do not add blocking waits (`.Result`, `.Wait()`) to hot paths.
- Preserve ordered packet parsing semantics in `CPacket`/`CMessageResolver`.

## Review checklist

- Any new mutable shared state has explicit synchronization.
- No deadlock-prone lock ordering introduced.
- Receive/send callback error behavior remains explicit.
- Logic-thread mode still serializes packet processing end-to-end.
