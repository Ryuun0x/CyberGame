# PSX_Boy2

A boy built in the ":time" (Koharu) style, from scratch. That model was only a visual reference; no mesh, rig or texture comes from it.

- **Body:** slim, long-legged and pale, about 6.8 heads tall.
- **Hair:** long and dark, in jagged layered panels: zigzag bangs, side curtains, a nape layer, crown spikes, and one high ponytail on two bones that sway when he moves.
- **Outfit:** a fitted striped tee with short sleeves and painted folds, a short dark skirt, a studded belt, crew socks and low sneakers.
- **Textures:** one 256 px atlas with baked ambient occlusion, plus 32 px pupil and mouth textures on their own quads.

| | |
|---|---|
| Triangles | 2,240 |
| Rig | 24 deform bones: humanoid, plus `ponytail.01` and `ponytail.02` |
| Clips | Idle 3.5 s, Walk 1.0 s, Run 0.73 s, all at 30 fps and in place |
| Files | `PSX_Boy2.blend`, `glb/PSX_Boy2.glb`, `textures/`, `renders/` |

## Rebuild

About 10 s headless.

```bash
"C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b --factory-startup --python ../scripts/psx/psx_boy2.py
```

## Changing the design

All of this lives in `scripts/psx/psx_boy2.py`.

- **Colours:** `COL.update(...)`.
- **Hair:** `build_hair`.
- **Outfit shapes:** the `TEE` and `SKIRT2` tables.
- **Textures:** `paint_atlas2`.
