# Manual smoke test prompt

Run a minimal manual smoke check for the sample client/server path:

1. Start server:
   - `dotnet run --project .\CSampleServer\CSampleServer.csproj`
2. Start client in a separate terminal:
   - `dotnet run --project .\CSampleClient\CSampleClient.csproj`
3. Send one chat line from client and confirm one `CHAT_MSG_ACK` response.

If the scenario uses a non-FreeNet client lacking heartbeat support, toggle server heartbeat off via the commented `service.disable_heartbeat()` line in `CSampleServer\Program.cs` as documented in `README.md`.
