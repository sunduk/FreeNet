# MCP setup for this repository

Use `.mcp.json.example` as a starter configuration for MCP-enabled clients.

## Preference

Use whichever method minimizes token usage first (CLI, MCP, or built-in tools), while preserving correctness. Use MCP when it is the lower-token practical path or when explicitly requested.

## Included servers

- `github`: repository/issue/PR context and automation
- `filesystem-freenet`: constrained file access rooted to this repository

## Quick setup

1. Copy `.mcp.json.example` to your client's MCP config location or rename to `.mcp.json` if your client reads it from repository root.
2. Set a token in your client/environment:
   - `GITHUB_PERSONAL_ACCESS_TOKEN` with repo access as needed.
3. Start/reload your MCP client.
