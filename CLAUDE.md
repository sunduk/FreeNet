# Claude/OpenCode repository instructions

Follow `AGENTS.md` as the primary repository instruction file.

Additional reminders for this codebase:
- Keep changes protocol-compatible across sample client/server and VirusWar server protocol enums.
- Preserve packet framing behavior (`HEADERSIZE = 4`, size-recording before send).
- Respect the logic-thread switch in `CNetworkService`; do not mix assumptions about thread context.
