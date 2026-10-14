import bpy

# Enable MCP for Blender addon, run on custom port (9876 is taken by devin-remote)
try:
    bpy.ops.preferences.addon_enable(module='blender_mcp')
    print("BlenderMCP: addon enabled")
except Exception as e:
    print(f"BlenderMCP: addon enable failed: {e}")

bpy.context.scene.blendermcp_port = 19876
bpy.context.scene.blendermcp_auto_start_server = True
