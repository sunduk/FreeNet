---
name: mcp-builder
description: Use when adding or improving MCP server configs/tools for this repository workflow.
license: Adapted from anthropics/skills mcp-builder guidance
---

# MCP Builder (Repo workflow edition)

Use for:
- creating MCP config files for local/dev clients
- adding GitHub/filesystem/playwright-style MCP servers
- designing tool schemas and usage guidance for coding agents

## Token-efficiency rule

Choose the method that minimizes total token usage first.

- Prefer concise, direct operations with smallest useful output.
- Use CLI, MCP, or built-in tools based on which is likely to return the least context for the task.
- If two options are equivalent in result quality, choose the lower-token path.

## Process

1. **Define workflows first**
   - Decide what tasks must be enabled (code search, PR/issue ops, local file operations, browser testing).
2. **Choose least-privilege server set**
   - Keep filesystem roots scoped to repo.
   - Keep token/env names explicit.
3. **Make configs portable**
   - Provide an example file (for this repo: `.mcp.json.example`).
   - Document required env vars and setup steps.
4. **Document usage with real tasks**
   - Include where each server helps in this repo.

## Tool design rules

- Use clear, action-oriented tool names.
- Support pagination/filtering on list-like operations.
- Return concise, structured data where possible.
- Surface actionable errors (missing auth, missing env, bad path scope).

## FreeNet defaults

- Use token-efficient workflows by default:
  - scope queries narrowly
  - request only required fields/results
  - avoid broad scans when targeted lookup is possible
- Keep MCP configs as optional integrations.
- If MCP is used, keep filesystem MCP rooted to this repository path.
