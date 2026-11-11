# Low-poly student — male character

Procedurally modelled, rigged and animated in Blender 5.2.3 from the reference sheets. The female variants are paused; their code is still in the scripts, and adding them back to `VARIANTS` in `lp_assemble.py` re-enables them.

## Deliverables

| Path | Contents |
|---|---|
| `LowPoly_Student_Male.blend` | Both variants, rigs, materials, 6 actions, preview stage. Each rig has NLA tracks `Idle` / `Walk` / `Run`, with Idle active. |
| `glb/Male_Casual.glb`, `glb/Male_Uniform.glb` | Skinned meshes, skeleton, and the clips `Idle`, `Walk`, `Run` (30 fps, in place). |
| `renders/*_turnaround.png` | Front / left / back / three-quarter views in the rest A-pose. |
| `renders/*_face.png` | Head front / left / right / three-quarter / back. |
| `renders/preview_*.mp4` | Idle, Walk and Run, both variants: a three-quarter view and side views for foot contact. |
| `scripts/` | Full generator. `run_all.py` rebuilds everything above. |

No texture files are needed. Every material is a flat colour, and the skull, crest, ID card, eyes, brows and lips are modelled as geometry.

## Report

| Variant | Body (head, face, hands) | Hair | Outfit | Backpack | **Total tris** | Bones |
|---|---|---|---|---|---|---|
| Male_Casual | 3004 | 910 | 2908 | 714 | **7536** | 22 |
| Male_Uniform | 3164 | 910 | 2018 | 714 | **6806** | 23 (+`id_card`) |

Both totals are over the 6k guideline because the head was rebuilt at higher detail on request: about 1.7k tris for the head alone, decimated from a dense mesh built from the reference profiles.

| Clip | Frames @30 fps | Duration | Notes |
|---|---|---|---|
| Idle | 105 | 3.5 s | Breathing, weight shift, head and arm drift; feet locked by IK |
| Walk | 30 | 1.0 s | Heel strike → roll → toe-off; stance foot locked to a ground scrolling at about 1.0 m/s |
| Run | 22 | 0.733 s | About 2.4 m/s, two airborne phases of about 0.1 s each, forward lean, high knee lift |

Each clip's last key equals its first, so the loops repeat seamlessly. The root stays at the origin, and the pelvis carries the bob and sway.

The GLBs were reimported into a fresh 30 fps scene. Both have one armature, four skinned meshes with their materials, and the three named clips, with lengths of 105, 30 and 22 frames. Bones move in every clip.

## Limitations

- **Hair:** a simplified, chunky version of the messy swept style; the reference has more irregular spikes.
- **Hands:** open, with no finger animation, as specified.
- **Skinning:** procedural envelope weights. The shoulder tops deform slightly when the arms are lowered or swung far back.
- **Jeans:** the wear is approximated with a few lighter facets.
- **Unity:** importing `.glb` needs glTFast or UnityGLTF. The files are outside `Assets/` so Unity doesn't import the `.blend`.

## Rebuild

```bash
"C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b --factory-startup --python scripts/run_all.py
```

To write the outputs somewhere else, set the `LP_OUT` environment variable to that folder first.
