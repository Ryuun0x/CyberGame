# Low-poly student (male) — v2, built on the provided base mesh

v1 is left untouched in `../LowPoly_Student_Male.blend`, `../glb/` and `../renders/`. Its scripts are snapshotted in `../scripts/v1_snapshot/`.

## What changed

The body and head come from `lowpoly_male_base_mesh-TPOSE.fbx`. That file is actually in an A-pose: watertight, symmetric, 964 tris.

- **Body:** scaled to 1.75 m, and the stance narrowed from about 51 cm between the feet to about 30 cm.
- **Rig:** joints are measured from cross-sections of the mesh (hip, knee, ankle, shoulder, elbow, wrist, spine and neck). The bone names and the Idle/Walk/Run clips are unchanged, re-baked for the new joints.
- **Weights:** Blender automatic (bone heat) weights on the whole body.
- **Clothes:** tee, undershirt, jeans, shirt, vest and trousers are copies of the body surface, cut with clean planes, offset outward, with folded hems. They inherit the body's weights, so cloth and skin deform together.
- **Shoulders:** the sleeves are part of the same surface as the torso. There is no separate tube passing through the body, so there's no crease or boxy cap.
- **Head:** the base head is subdivided once. It is pulled about 55% (80% for the chin and jaw) toward a surface built from the reference's front and side profiles. The fin ears are replaced with modelled ears (rim plus concha). Eyes, lids, lash line, raised brows, nostril shading and lips are projected onto the surface.
- **Hair:** rebuilt as broad swept panels, kept low on the head, fitted to the base head's surface.
- **Skull print:** a PNG texture on the tee (UV-mapped front panel) instead of overlay triangles.
- **Shading:** skin is smooth-shaded with folds above 80° kept sharp. Everything else is flat-shaded.

## Numbers

| Variant | Body | Hair | Outfit | Backpack | **Total tris** | Bones |
|---|---|---|---|---|---|---|
| Male_Casual | 1307 | 1230 | 1584 | 714 | **4835** | 22 |
| Male_Uniform | 1385 | 1230 | 1616 | 714 | **4945** | 23 |

Clips at 30 fps, in place:

| Clip | Frames | Duration |
|---|---|---|
| Idle | 105 | 3.5 s |
| Walk | 30 | 1.0 s |
| Run | 22 | 0.733 s |

The GLBs were reimported: one armature, 4 skinned meshes with their materials, 3 named clips that each move the bones. The casual tee texture is embedded.

## Files

| Path | Contents |
|---|---|
| `LowPoly_Student_Male_v2.blend` | Both variants, rigs, NLA clips, packed tee texture |
| `glb/Male_Casual.glb`, `glb/Male_Uniform.glb` | Exports |
| `textures/Tee_Skull_Print.png` | Skull print (also embedded in the .blend and the casual GLB) |
| `renders/*_before_after_body.png`, `renders/*_before_after_head.png` | v1 (top) vs v2 (bottom): front / side / back / three-quarter orthographic views, plus head close-ups |
| `renders/Male_Casual_vs_reference.png` | Reference sheet above the v2 renders |
| `renders/*_turnaround.png`, `renders/*_face.png` | Final sheets |
| `renders/deformation_run_closeups.png` | Shoulders, elbows, straps and knees at the extreme run poses |
| `renders/preview_*.mp4` | Idle, Walk and Run: three-quarter and side views |
| `renders/before/diag_shoulders.png` | v1 shoulder diagnosis. The shoulder "openings" were not holes but a sleeve-tube crease. |

## Remaining issues

- **Hair:** still a smoother, rounder mass than the reference's messy swept spikes.
- **Face:** simpler than the reference. The eye sockets are shallower and the expression more neutral. Eyes and brows are surface decals rather than modelled lids.
- **Hands:** the base hands (thumb plus grouped fingers) are proportionate but less detailed than the reference.
- **Run legs:** the leg IK misses its target by up to about 1.8 cm at the run extremes (walk about 0.6 cm), so a foot may lift slightly. The base body's legs are shorter than my v1 rig's, and the run's foot paths weren't rescaled for that.
- **Skin weights:** automatic only, with no hand painting. They look clean in the checked poses, but extreme shoulder raises weren't tested.
- **Female variants:** paused.

## Rebuild

```bash
"C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b --factory-startup --python ../scripts/run_all.py
```

Set `LP_OUT` to this `v2` folder and `LP_BLEND=LowPoly_Student_Male_v2.blend` first. `LP_BASE_FBX` overrides the base mesh path.
