# Face transplant option (Po-uta face on our male student)

This is an alternative to the base-head version in `../`. That version is unchanged; compare the two before choosing.

## How it was made (`scripts/lp_face_import.py`, used by `lp_base.py`)

1. Import `Po-uta.fbx`, an illustration scene. Take the character's head skin and its eye cards from `porter.001`.
2. Un-pose the head. The source is baked in a tilted pose with no skeleton driving it, so the orientation comes from the face itself:
   - **Side axis:** the two eye clusters.
   - **Forward:** from the hair mass to the eyes.
   - **Up:** away from the neck.
   - **Pitch:** a 25° nod correction tuned by eye.
3. Scale it to our head height (chin to crown, 0.242 m), put the eye line at our eye level, cut the neck at 1.462 m (under the collar), and cap the open back of the skull.
4. Texture:
   - Warm the painted pale skin halfway toward our skin tone; eye whites and highlights are kept.
   - Our body skin is set to the same tone.
   - Repaint the blond/green hair strokes on the scalp, temples and nape to our auburn.
   - Downscale to 1024², stored as `textures/Face_Skin.png` and packed in the .blend.
5. Weight it to head and neck, and refit our hair to whichever is outermost, the transplanted skull or the reference head.

## Numbers

| Variant | Total tris | Bones | Clips | GLB |
|---|---|---|---|---|
| Male_Casual | 8771 (about 4k of it is the transplanted face) | 22 | Idle / Walk / Run | 2.2 MB |
| Male_Uniform | 8881 | 23 | Idle / Walk / Run | 2.2 MB |

## Known issues

- **Style:** the source is a smooth anime face (big painted eyes, small nose), not low-poly. It doesn't match the low-poly body or your realistic reference sheet.
- **Scalp repaint:** a small brown patch sits beside one eye corner, a painted strand runs down the nape, and a little green tint remains near the ears.
- **Hairline:** the forehead is taller than our hair expects, so the repainted hairline shows under the fringe as a band.
- **Triangles:** over budget, because the face isn't decimated. Decimating it would blur its UVs and painted features.
- **Licence:** the zip contains no licence file. Check the original Po-uta page's licence and credit terms before using this face in a shipped game.

## Switching

`LP_FACE=0` builds the base-head version, `LP_FACE=1` (default when the FBX exists) builds this one. `LP_FACE_FBX` and `LP_FACE_TEX` override the source paths.
