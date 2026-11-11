"""Headless: arm cross-section profile (radius + centre offset from the bone line) for the v3 male, rest pose."""
import bpy, os
from mathutils import Vector as V
OUT = os.environ.get('LP_DIAG_OUT', '.')
sc = bpy.context.scene; rig = next(o for o in sc.objects if o.type == 'ARMATURE')
B = lambda n, t=False: rig.matrix_world @ (rig.data.bones[n].tail_local if t else rig.data.bones[n].head_local)
S, E, W, H = B('upper_arm.L'), B('forearm.L'), B('hand.L'), B('hand.L', True)
rig.data.pose_position = 'REST'; bpy.context.view_layer.update()
dg = bpy.context.evaluated_depsgraph_get()
lines = [f"S {tuple(round(x,3) for x in S)} E {tuple(round(x,3) for x in E)} W {tuple(round(x,3) for x in W)}"]
for name in ('Male_Casual_Clothes', 'Male_Body'):
    o = bpy.data.objects[name]; me = o.evaluated_get(dg).to_mesh(); ws = [o.matrix_world @ v.co for v in me.vertices]
    lines.append(f"-- {name}")
    for seg, (a, b), rng in (('upper', (S, E), [i / 10 for i in range(-2, 11)]), ('fore', (E, W), [i / 10 for i in range(0, 11)]), ('hand', (W, H), [0.0, 0.5, 1.0])):
        ab = b - a; L = ab.length; d = ab.normalized()
        for t in rng:
            c = a + ab * t
            sl = [p for p in ws if abs((p - c).dot(d)) < 0.012 and p.x > 0.1 and ((p - c) - d * (p - c).dot(d)).length < 0.09]
            if not sl: lines.append(f"{seg} {t:+.1f}: none"); continue
            rad = [((p - c) - d * (p - c).dot(d)) for p in sl]
            cen = sum(rad, V()) / len(rad)
            lines.append(f"{seg} {t:+.1f}: n={len(sl):3d} rmax={max(r.length for r in rad):.3f} rmean={sum(r.length for r in rad)/len(rad):.3f} centre_off={cen.length:.3f} ({cen.x:+.3f},{cen.y:+.3f},{cen.z:+.3f})")
    o.evaluated_get(dg).to_mesh_clear()
open(os.path.join(OUT, 'arm_profile.txt'), 'w').write('\n'.join(lines))
