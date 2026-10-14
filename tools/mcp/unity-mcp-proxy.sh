#!/usr/bin/env bash
# stdio -> streamable-HTTP proxy for the persistent gamedev-mcp-server on 127.0.0.1:29715
# (single server instance serves both the Unity plugin hub and MCP clients)
export NVM_DIR="$HOME/.nvm"; . "$NVM_DIR/nvm.sh" >/dev/null 2>&1
exec npx -y mcp-remote http://localhost:29715/mcp
