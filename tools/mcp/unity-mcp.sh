#!/usr/bin/env bash
# Unity-MCP persistent server guard for this VM.
# Ensures gamedev-mcp-server is listening on 127.0.0.1:29715 (streamable HTTP).
# The Unity-MCP plugin (connectionMode: Custom) connects here from inside the editor.
# MCP clients (Devin sessions) use transport: streamable-http, url: http://localhost:29715/mcp

PORT=29715
SERVER_BIN="$HOME/repos/blender-unity-devin/AAA-Game/Library/mcp-server/linux-x64/gamedev-mcp-server"
LOG=/tmp/gamedev-server.log

if curl -s --max-time 2 -o /dev/null "http://localhost:${PORT}/mcp" -X POST \
    -H "Content-Type: application/json" \
    -d '{"jsonrpc":"2.0","id":0,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"guard","version":"1"}}}'; then
  echo "unity-mcp server already running on ${PORT}"
  exit 0
fi

if [ ! -x "$SERVER_BIN" ]; then
  # fallback to the CLI-managed copy
  SERVER_BIN="$HOME/.ai-game-dev/server/linux-x64/gamedev-mcp-server"
fi

echo "starting gamedev-mcp-server on ${PORT} ..."
nohup "$SERVER_BIN" "port=${PORT}" plugin-timeout=10000 \
  client-transport=streamableHttp authorization=none >> "$LOG" 2>&1 &
sleep 3
ss -tln | grep -q ":${PORT} " && echo "listening on ${PORT}" || { echo "FAILED"; tail -5 "$LOG"; exit 1; }
