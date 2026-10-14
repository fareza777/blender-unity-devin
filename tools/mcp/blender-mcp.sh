#!/bin/bash
export PATH="$HOME/.local/bin:$PATH"
export BLENDER_HOST=localhost
export BLENDER_PORT=19876
export BLENDER_MCP_DISABLE_TELEMETRY=1
export BLENDERMCP_NO_UPDATE_CHECK=1
exec uvx blender-mcp
