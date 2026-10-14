# blender-unity-devin

VM untuk bikin game kualitas AAA dengan **AI-driven Unity Editor + Blender** via MCP (Model Context Protocol).

## Stack

| Komponen | Versi | Lokasi |
|---|---|---|
| Unity Editor | 6000.0.84f1 (Unity 6 LTS) | `~/Unity/Hub/Editor/6000.0.84f1/` |
| Unity Hub | 3.22.3 (AppImage) | `~/Applications/Unity Hub.AppImage` |
| Unity-MCP plugin | 0.93.4 | `AAA-Game/Packages/manifest.json` → `com.ivanmurzak.unity.mcp` |
| gamedev-mcp-server | 9.2.11 | `AAA-Game/Library/mcp-server/linux-x64/` + `~/.ai-game-dev/` |
| unity-mcp-cli | 0.93.4 | npm global (node via nvm) |
| Blender | 4.5.9 LTS | `~/blender/` |
| blender-mcp | latest (PyPI, via `uvx blender-mcp`) | addon `~/.config/blender/4.5/scripts/addons/blender_mcp.py` |

## Cara kerja

```
Devin session / MCP client
  ├─ streamable-http → http://localhost:29715/mcp ── gamedev-mcp-server ──(hub)──> Unity Editor plugin
  │                                                  (38 tools: gameobject-create, assets-*, console-get-logs, dll)
  └─ stdio → ~/mcp-servers/blender-mcp.sh ── uvx blender-mcp ──(TCP 19876)──> Blender addon
                                                  (tools: get_scene_info, execute_code, create_object, dll)
```

- **Unity**: server listen di `127.0.0.1:29715` (`authorization=none`, bind loopback). Plugin di dalam editor connect ke situ via `connectionMode: Custom` (local mode, bukan cloud) — config di `AAA-Game/UserSettings/AI-Game-Developer-Config.json` (file ini **di-gitignore**, isinya token lokal).
- **Blender**: addon listen di `127.0.0.1:19876` (bukan default 9876 — dipakai proses Devin). Auto-start via `~/mcp-servers/blender_startup.py`.

## Menjalankan

```bash
# Unity editor (GUI)
DISPLAY=:0 ~/Unity/Hub/Editor/6000.0.84f1/Editor/Unity -projectPath ~/repos/blender-unity-devin/AAA-Game &

# gamedev-mcp-server (guard — jalan kalau belum listen)
~/mcp-servers/unity-mcp.sh

# Blender (GUI)
DISPLAY=:0 ~/blender/blender --python ~/mcp-servers/blender_startup.py &

# cek status end-to-end
unity-mcp-cli status ~/repos/blender-unity-devin/AAA-Game
```

## MCP config untuk Devin session / MCP client lain

```json
{
  "unity":    { "type": "streamable-http", "url": "http://localhost:29715/mcp" },
  "blender":  { "command": "/home/ubuntu/mcp-servers/blender-mcp.sh" }
}
```

## Catatan

- **Lisensi Unity Personal**: sudah aktif di VM ini (`~/.config/unity3d/.../UnityEntitlementLicense.xml`). Kalau VM/snapshot baru → sign in ulang lewat Unity Hub GUI (email + password, sekali saja).
- **Secrets** (ElevenLabs, Replicate, Recraft, Meshy, Unity ID) ada sebagai Devin secrets — dipakai via env var, jangan di-commit.
- Scene `Assets/Scenes/SampleScene.unity` — test object `MCP_TestCube` dibuat via tool `gameobject-create` (bukti E2E).
