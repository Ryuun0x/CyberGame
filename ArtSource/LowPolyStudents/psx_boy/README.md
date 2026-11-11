# PSX_Boy

An original PS1-style character: a slim, long-legged teen boy with long, messy dark hair (spiky crown), pale skin, an oversized gray hoodie, a pleated black skirt, striped knee socks and chunky sneakers. The look follows evanciao's Neto and ":time" (slim proportions, a small soft-filtered texture, a painted face with separate pupil and mouth planes). It's built from scratch; no geometry, rig or texture comes from those works.

The Koharu model in `Documents/Codex/.../outputs/Koharu_Unity` was extracted from the ":time" game build (the AssetRipper `ExportedProject`). It's all-rights-reserved game art, so it was used only as a visual reference and is not part of this character.

| | |
|---|---|
| Triangles | **1,696**: body, clothes, hair, plus 3 face planes (pupils ×2, mouth) |
| Textures | `PSX_Boy_Atlas.png`: 256×256, linear filtering; the face (whites, lashes, brows) is painted at 64 px. `PSX_Boy_Pupil.png` and `PSX_Boy_Mouth.png`: 32×32 with alpha, on their own quads, so you can swap them for expressions or animate them in Unity. |
| Rig | 22 deform bones plus `root`, A-pose |
| Clips | Idle 3.5 s, Walk 1.0 s, Run 0.73 s, all at 30 fps and in place. The stride is shortened for the skirt. |

## Files

| Path | Contents |
|---|---|
| `PSX_Boy.blend` | The character, rig and clips |
| `glb/PSX_Boy.glb` | The game export, texture embedded |
| `renders/turnaround.png`, `renders/face.png`, `renders/filmstrip_*.png` | Review renders |

## Rebuild

About 20 s headless. Set `LP_VIDEOS=1` to also render preview videos.

```bash
"C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b --factory-startup --python ../scripts/psx/psx_boy.py
```

## Changing the design

- **Colours:** `COL` in `psx_boy.py`.
- **Face:** `paint_face` (whites, lashes, brows), `_pupil` and `_mouth` (separate textures), and `build_face_planes` (their placement).
- **Hair locks:** `build_hair`.
- **Hoodie and skirt shapes:** the `HOODIE` and `SKIRT` ring tables.
