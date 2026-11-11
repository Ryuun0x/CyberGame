# Male student v3: PS1-style low-poly, two outfits, one rig

This is a new version. v1 (`../LowPoly_Student_Male.blend`) and v2 (`../v2/`) are untouched. v3 follows the reference sheets and the PS1-style face reference. It has a low-poly mesh, low-resolution pixel textures with nearest-pixel filtering, and clothes painted onto one surface with the skin. There's no backpack, ID card or lanyard.

Built and checked in Blender 5.2.3: driven live over MCP while developing, then rebuilt headless from the scripts.

## Files

| Path | Contents |
|---|---|
| `Male_Student_v3.blend` | Character, both outfits, rig, 3 actions. Textures are packed. It also holds a `README` text block. |
| `glb/Male_Casual.glb`, `glb/Male_Student.glb` | Dressed character, skeleton, and the clips `Idle`, `Walk`, `Run`. Textures are embedded and use NEAREST filtering. |
| `textures/` | `Face_Skin.png` (80 px pixel face), `Pixel_Casual_Clothes.png`, `Pixel_Student_Clothes.png`, `Pixel_Hair.png` (baked pixel atlases) |
| `renders/*_turnaround.png` | Front / left / right / back / 3/4 views in the rest A-pose |
| `renders/*_face.png` | Enlarged head: front / left / right / 3/4 / back |
| `renders/filmstrip_*.png` | 6 frames of each clip, casual (top) and student (bottom) |
| `renders/preview_*.mp4` | Both outfits side by side: Idle 3/4, Walk and Run in 3/4 and side views |
| `report.json` | Triangle counts, clip lengths, GLB reimport results |

## Arm and head fix (2026-10-27)

The previous build is kept in `before_armfix/`.

- **Arms:** the base mesh had a knob at each elbow (radius 6.7 cm between 4.5 cm sections, sitting 2 cm off the bone). That made the forearm read as a separate piece. `reshape_arms` in `v3_body.py` re-profiles the base arm before weighting, as one smooth taper centred on the bones: 5.6 cm at the shoulder, 4.0 cm at the elbow and 2.7 cm at the wrist (`ARM_R`). The skin, the sleeves and both outfits all inherit it.
- **Head and neck:** proportions moved toward the Neto reference (evanciao, PS1 style).
  - **Head:** `HEAD_H` goes from 0.255 to 0.285 m, so he's about 5.9 heads tall.
  - **Chin:** `CHIN_Z` drops from 1.40 to 1.392, which shortens the visible neck.
  - **Neck:** the neck rings are about 25% thicker.
  - **Rig:** the neck and head bones follow `CHIN_Z` and `HEAD_H`. Hair and face paint already scale with the head.
- **Rebuilding elsewhere:** `run_v3.py` can now write anywhere. Set `LP_V3` for the output and `LP_V3_SCRIPTS` for the script folder.

## Organisation

All of it lives under the `Male_Character` collection, with one `Male_Rig`:

- **Base**: `Male_Body` (head, thin neck, hands) and `Male_Hair`. The ears are under the hair.
- **Outfit_Casual**: `Male_Casual_Clothes`: one painted surface (olive tee with skull, rust sleeves, hem and crew neck, jeans) plus boots.
- **Outfit_Student**: `Male_Student_Clothes`: one painted surface (shirt, vest, V and neckline trim, crest, tie, collar points, trousers, and the bare arms below the sleeves) plus shoes.

To switch outfits, show exactly one `Outfit_` collection.

## Report

| | Casual | Student |
|---|---|---|
| Body (head, hands) | 806 | 806 |
| Hair | 882 | 882 |
| Outfit | 2432 | 2412 |
| **Total tris** | **4120** | **4100** |

**Rig:** 22 deform bones plus `root`. The chain is root, pelvis, spine, chest, neck and head; each side has clavicle, upper_arm, forearm, hand, thigh, shin, foot and toe. The rest pose is an A-pose.

**Clips** (30 fps, in place, root fixed):

| Clip | Frames | Duration |
|---|---|---|
| Idle | 105 | 3.5 s |
| Walk | 30 | 1.0 s |
| Run | 22 | 0.733 s |

**GLB verification:** both files were reimported. Each came back with 1 armature (22 bones) and every mesh skinned with its materials and embedded textures. All three clips are present at the right lengths, with 53–62 animated channels each. At the loop seam the first and last keys are identical (max delta 0). No export warnings.

## How it's built (`../scripts/v3/`)

- **Pixel textures** (`v3_pixel.py`):
  1. Each face is projected along its dominant axis.
  2. Blender packs the islands without rotation.
  3. Every triangle is rasterised in UV space, and each texel's 3D point is coloured by the outfit's rules (V-neck, trims, neckline, armholes, crest, tie, skull).

  Because the colour rules are sampled per texel at about 4.5 mm, slanted edges become pixel stairs. Filtering is "Closest".
- **Outfits** (`v3_outfits.py`): one thin surface per outfit, 1.5–3 mm off the skin. It's cut from the weighted body, so cloth and skin bend together. It continues past every fabric edge (up the neck, down the bare arms, to the wrists) painted as skin. The sleeve ends, neckline and cuffs are therefore pixel edges in the texture, with a dark-brown pixel line and a shade line on the skin side. There are no layers and no collar piece. Every hem (sleeves, cuffs, vest band, tee and undershirt hems, trouser and jeans hems, necklines) has a 2-pixel darker shade of its own colour. On the arms, weights blend evenly from upper arm to forearm across the elbow and into the hand at the wrist, and the arms are smooth-shaded, so each arm bends as one limb.
- **Body** (`v3_body.py`): the base mesh scaled to a teen, with the sway-back straightened (`BACK_S`) and the wedge-shaped chest rounded (`CHEST_W`) before bone-heat weighting.
- **Head and face** (`v3_head.py`): a 12-segment loft, so the facets show. It's set back over the shoulders so the thin neck rises straight; below the chin the neck is the outfit surface. The face is 80 px pixel art: big dark eyes under a heavy lash line, straight brows, blush marks.
- **Hair** (`v3_hair.py`): a puffy anime mass with chunky bangs, side locks standing off the cheeks, spiky tufts on top, and strands covering the ears. A pixel texture adds the odd darker strand.
- **Hands**: a palm, a thumb and four 3-segment fingers, continuous with the arm.
- **Animation** (`v3_anim.py`): adapted from v2's `lp_anim.py`. Legs use analytic two-bone IK on a heel/ball roll model.

**Rebuild** (a few minutes; set `LP_VIDEOS=0` to skip the videos):

```bash
"C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b --factory-startup --python scripts/v3/run_v3.py
```

The base mesh is read from `C:\Users\pajar\Downloads\low-poly-male-base-mesh\source\lowpoly_male_base_mesh-TPOSE.fbx`. Set `LP_BASE_FBX` to override it.

## Limitations

- **Run foot contact:** the leg IK misses its target by up to 1.2 cm at the run's extremes, so a foot can hover slightly for a frame. In the walk the miss is 0.35 cm.
- **In-place locomotion:** the stance foot slides back at the walking speed. That's intended for root-motion-free game use.
- **Pixel grid:** the texture grid follows each projection island, so pixel stairs line up with the body's axes. The pixel size is the same everywhere, which makes the crest a small pixel badge.
- **Hair:** procedural, so it is a simplified shape rather than a hand-modelled haircut.
- **Wrists:** the hands are a separate smooth mesh, so a faint seam shows where the outfit surface ends at the wrist.
- **Skinning:** automatic bone-heat weights with no hand painting. Checked in the walk and run extremes; extreme arm raises weren't tested.
- **Not reviewed in the videos:** I inspected stills and filmstrips; I can't play the mp4s.
- **Base mesh licence:** the base FBX was supplied by you. Its licence wasn't checked.
