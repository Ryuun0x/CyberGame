"""Procedural Idle / Walk / Run loops, baked as per-frame quaternion keys (exec'd after lp_assemble.py).

Each bone's pose is authored as a rotation q_b in armature-space axes, relative to its
parent's posed frame. Pose-bone basis = R_rest^-1 @ q_b @ R_rest.
Legs use analytic 2-bone IK toward ankle targets derived from a heel/ball roll model, so the
stance foot stays locked to a ground that scrolls back at the virtual walking speed.
"""
X, Y, Z = V((1, 0, 0)), V((0, 1, 0)), V((0, 0, 1))
def Q(axis, deg): return Quaternion(axis, math.radians(deg))
def mirq(q): return Quaternion((q.w, q.x, -q.y, -q.z))
def ease(t): t = max(0.0, min(1.0, t)); return t * t * (3 - 2 * t)
def wrap(p): return p - math.floor(p)
TAU = 2 * math.pi

CLIPS = {'Idle': dict(T=105), 'Walk': dict(T=30), 'Run': dict(T=22)}

def catmull(pts, knots, s):
    for i in range(len(knots) - 1):
        if s <= knots[i + 1] or i == len(knots) - 2: break
    t = (s - knots[i]) / (knots[i + 1] - knots[i])
    p0 = pts[max(i - 1, 0)]; p1 = pts[i]; p2 = pts[i + 1]; p3 = pts[min(i + 2, len(pts) - 1)]
    t2, t3 = t * t, t * t * t
    return 0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2 + (-p0 + 3 * p1 - 3 * p2 + p3) * t3)

class Gait:
    """Foot placement model for one character."""
    def __init__(s, J, clip, skirt=False):
        s.J = J; s.skirt = skirt; A0 = J['foot.L'][0]; B0 = J['foot.L'][1]
        s.ank_h = A0.z; s.heel = 0.062; s.ball = A0.y - B0.y        # heel behind ankle, ball ahead of ankle
        L = (J['thigh.L'][1] - J['thigh.L'][0]).length + (J['shin.L'][1] - J['shin.L'][0]).length
        s.L = L; k = L / 0.845
        if clip == 'Walk':
            s.stance, s.Ds, s.strike, s.toeoff, s.lift = 0.6, 0.6 * k, -15.0, 36.0, 0.05
            s.roll = (0.12, 0.55)
        else:
            s.stance, s.Ds, s.strike, s.toeoff, s.lift = 0.36, 0.64 * k, -6.0, 44.0, 0.0
            s.roll = (0.15, 0.42)
        s.clip = clip; s.y0 = -s.Ds / 2 - 0.015 * k

    def stance_foot(s, u):
        """u in [0,1] of stance -> (ankle_y, ankle_z, pitch, toe_rel) with ground scrolling +Y."""
        ya = s.y0 + s.Ds * u
        r0, r1 = s.roll
        if u < r0: pitch = lerp(s.strike, 0.0, ease(u / r0))
        elif u < r1: pitch = 0.0
        else: pitch = s.toeoff * ((u - r1) / (1 - r1)) ** 1.6
        A = V((0, ya, s.ank_h))
        if pitch < 0: piv = V((0, ya + s.heel, 0))
        else: piv = V((0, ya - s.ball, 0))
        Ap = piv + Q(X, pitch) @ (A - piv)
        return Ap.y, Ap.z, pitch, (-pitch if pitch > 0 else 0.0)

    def foot(s, p):
        """phase p in [0,1), heel strike at 0 -> ankle (y,z), pitch, toe_rel, in_stance."""
        if p < s.stance:
            y, z, pt, tr = s.stance_foot(p / s.stance); return y, z, pt, tr, True
        sw = (p - s.stance) / (1 - s.stance)
        y0, z0, p0, t0 = s.stance_foot(1.0); y1, z1, p1, _ = s.stance_foot(0.0)
        if s.clip == 'Walk':
            y = lerp(y0, y1, ease(sw) * 0.85 + sw * 0.15)
            z = lerp(z0, z1, ease(sw)) + s.lift * math.sin(math.pi * sw ** 0.8) + 0.05 * math.sin(math.pi * min(1.0, sw * 1.8)) * (1 - sw)
            pitch = lerp(p0, 4.0, ease(sw / 0.4)) if sw < 0.4 else lerp(4.0, p1, ease((sw - 0.4) / 0.6))
        else:
            k = s.L / 0.845; kk = 0.8 if s.skirt else 1.0      # skirt: lower knee lift so the thigh stays under the hem
            pts = [V((y0, z0)), V((y0 + 0.06 * k, 0.40 * k * kk)), V((y1 - 0.12 * k * kk, 0.36 * k * kk)), V((y1 - 0.02 * k, 0.13 * k)), V((y1, z1))]
            q = catmull(pts, [0, 0.28, 0.6, 0.88, 1.0], sw)
            y, z = q.x, q.y
            pitch = lerp(p0, -8.0, ease(sw / 0.5)) if sw < 0.5 else lerp(-8.0, p1, ease((sw - 0.5) / 0.5))
        return y, z, pitch, t0 * (1 - ease(sw / 0.3)), False

def solve_leg(J, side, Qp, pel_pos, target, pitch, toe_rel):
    H0, K0 = J['thigh.' + side]; A0 = J['shin.' + side][1]; P0 = J['pelvis'][0]
    Hw = pel_pos + Qp @ (H0 - P0)
    L1, L2 = (K0 - H0).length, (A0 - K0).length
    d = target - Hw; dist = min(max(d.length, 0.25 * (L1 + L2)), (L1 + L2) * 0.9995); dn = d.normalized()
    fw = Qp @ V((0, -1, 0)); pp = (fw - dn * fw.dot(dn)).normalized()
    ca = max(-1.0, min(1.0, (L1 * L1 + dist * dist - L2 * L2) / (2 * L1 * dist))); a = math.acos(ca)
    knee = Hw + (dn * math.cos(a) + pp * math.sin(a)) * L1
    ank = Hw + dn * dist
    r1, r2 = (K0 - H0).normalized(), (A0 - K0).normalized()
    Qt = (Qp @ r1).rotation_difference((knee - Hw).normalized()) @ Qp
    Qs = (Qt @ r2).rotation_difference((ank - knee).normalized()) @ Qt
    Qf = Q(X, pitch)
    return {'thigh.' + side: Qp.inverted() @ Qt, 'shin.' + side: Qt.inverted() @ Qs,
            'foot.' + side: Qs.inverted() @ Qf, 'toe.' + side: Q(X, toe_rel)}, (d.length - dist)

def reach_height(J, side, Qp, pel_xy, target):
    """Highest pelvis z that still lets this leg reach target with a near-straight knee."""
    H0 = J['thigh.' + side][0]; P0 = J['pelvis'][0]
    off = Qp @ (H0 - P0)
    L = ((J['thigh.L'][1] - J['thigh.L'][0]).length + (J['shin.L'][1] - J['shin.L'][0]).length) * 0.992
    hx, hy = pel_xy[0] + off.x - target.x, pel_xy[1] + off.y - target.y
    h2 = L * L - hx * hx - hy * hy
    return target.z + math.sqrt(max(h2, 0.0)) - off.z

def arm_rots(J, side, swing, adduct, elbow, wrist=-4.0, twist=0.0):
    """Left-side authoring; right side passes through mirq."""
    d2 = (J['forearm.L'][1] - J['forearm.L'][0]).normalized()
    hinge = d2.cross(Y).normalized()
    dh = (J['hand.L'][1] - J['hand.L'][0]).normalized(); hh = dh.cross(Y).normalized()
    up = Q(X, swing) @ Q(Y, adduct) @ Q((J['upper_arm.L'][1] - J['upper_arm.L'][0]).normalized(), twist)
    fo = Q(hinge, -elbow); ha = Q(hh, wrist)
    r = {'upper_arm.L': up, 'forearm.L': fo, 'hand.L': ha}
    if side == 'R': r = {k[:-2] + '.R': mirq(v) for k, v in r.items()}
    return r

def body_pose(J, P, clip, p, extra):
    """Returns (rotations dict, pelvis offset) for phase p in [0,1)."""
    fem = P['sex'] == 'F'; R = {}; c, sn = math.cos(TAU * p), math.sin(TAU * p)
    P0 = J['pelvis'][0]
    if clip == 'Idle':
        Qp = Q(Z, 1.5 * math.sin(TAU * p + 0.4)) @ Q(Y, -1.8 * sn) @ Q(X, 1.5)
        off = V((0.014 * sn, 0.004 * math.sin(TAU * p * 2), 0))
        tgt = {'L': V((J['foot.L'][0].x + 0.012, J['foot.L'][0].y - 0.015, J['foot.L'][0].z)),
               'R': V((J['foot.R'][0].x - 0.012, J['foot.R'][0].y + 0.02, J['foot.R'][0].z))}
        z = min(reach_height(J, sd, Qp, (off.x, off.y), tgt[sd]) for sd in 'LR')
        off.z = min(z, P0.z - 0.012) - P0.z
        pel = P0 + off
        for sd in 'LR':
            r, _ = solve_leg(J, sd, Qp, pel, tgt[sd], 0.0, 0.0); R.update(r)
        br = math.sin(TAU * p)                   # one slow breath per loop
        R['spine'] = Q(Y, 1.4 * sn) @ Q(X, 1.0 - 0.5 * br)
        R['chest'] = Q(Y, 0.8 * sn) @ Q(X, -1.2 * br) @ Q(Z, -1.0 * math.sin(TAU * p + 0.4))
        R['neck'] = Q(X, -1.0 + 0.6 * br)
        R['head'] = Q(Z, 4.0 * math.sin(TAU * p + 0.9)) @ Q(X, 1.5 * math.sin(TAU * 2 * p + 0.3)) @ Q(Y, -1.0 * sn)
        for sd, sg in (('L', 1), ('R', -1)):
            R['clavicle.' + sd] = (Q(Y, -1.6 * br) if sd == 'L' else mirq(Q(Y, -1.6 * br)))
            ph = TAU * p + (0 if sd == 'L' else 1.7)
            R.update(arm_rots(J, sd, -2.0 + 2.2 * math.sin(ph), 19.0 + 1.5 * math.sin(ph + 0.5), 13 + 3 * math.sin(ph + 1.0), -6, -8))
        pony = 2.0 * math.sin(TAU * p + 1.2); pside = 2.5 * math.sin(TAU * p + 2.0); card = -1.0 - 1.0 * (1 + math.sin(TAU * p))
    else:
        run = clip == 'Run'; g = extra['gait']
        yaw = (7.0 if run else 5.0); roll = (3.5 if run else 3.0)
        Qp = Q(Z, -yaw * c) @ Q(Y, -roll * sn) @ Q(X, 8.0 if run else 3.0)
        feet = {}
        for sd, ph in (('L', p), ('R', wrap(p + 0.5))):
            y, z, pitch, toe, st = g.foot(ph)
            ax = J['foot.' + sd][0].x
            feet[sd] = (V((ax, y, z)), pitch, toe, st)
        sway = (0.012 if run else 0.018) * sn
        if run:
            zz = P0.z - 0.06 * (g.L / 0.845) - 0.026 * math.cos(TAU * 2 * (p - 0.18))
        else:
            zz = P0.z + 0.003
        for sd in 'LR':
            if feet[sd][3]: zz = min(zz, reach_height(J, sd, Qp, (sway, 0.0), feet[sd][0]))
        extra.setdefault('zraw', {})[p] = zz
        off = V((sway, 0.0, (extra['zs'][p] if 'zs' in extra else zz) - P0.z))
        pel = P0 + off
        for sd in 'LR':
            t, pitch, toe, _ = feet[sd]
            r, miss = solve_leg(J, sd, Qp, pel, t, pitch, toe); R.update(r)
            extra.setdefault('miss', []).append(miss)
        lean = 6.0 if run else 2.0
        R['spine'] = Q(Z, yaw * 0.55 * c) @ Q(Y, roll * 0.6 * sn) @ Q(X, lean + (1.5 if run else 0.8) * math.cos(TAU * 2 * p))
        R['chest'] = Q(Z, yaw * (0.9 if run else 0.75) * c) @ Q(Y, roll * 0.3 * sn) @ Q(X, 2.0 if run else 0.5)
        tot_pitch = (8.0 if run else 3.0) + lean + (2.0 if run else 0.5)
        R['neck'] = Q(Z, yaw * 0.05 * c) @ Q(X, -tot_pitch * 0.45)
        R['head'] = Q(Z, -yaw * 0.15 * c) @ Q(X, -tot_pitch * 0.45 + (1.2 if run else 0.8) * math.cos(TAU * 2 * p + 0.6)) @ Q(Y, -roll * 0.6 * sn)
        for sd, ph in (('L', p), ('R', wrap(p + 0.5))):
            cc = math.cos(TAU * ph)
            if run:
                R.update(arm_rots(J, sd, -10.0 + 34.0 * cc, 23.0, 82.0 - 14.0 * cc, -10, -18))
            else:
                R.update(arm_rots(J, sd, -2.0 + 16.0 * cc, 20.0, 9.0 - 6.0 * cc, -6, -10))
            R['clavicle.' + sd] = (Q(Z, 2.5 * cc) if sd == 'L' else mirq(Q(Z, 2.5 * cc)))
        k = 2.2 if run else 1.0
        pony = (9.0 if run else 3.0) + 5.0 * k * math.cos(TAU * 2 * p + 0.9) ; pside = 4.0 * k * math.sin(TAU * p + 0.6)
        card = -(2.0 if run else 1.0) - (3.0 if run else 1.2) * (1 + math.cos(TAU * 2 * p + 1.0))
    R['pelvis'] = Qp
    if fem:
        R['ponytail.01'] = Q(Y, pside) @ Q(X, pony)
        R['ponytail.02'] = Q(Y, pside * 0.8) @ Q(X, pony * 0.7)
    R['id_card'] = Q(X, card)
    return R, off

def bake_clip(rig, J, P, clip):
    T = CLIPS[clip]['T']; extra = {}
    if clip != 'Idle':
        extra['gait'] = Gait(J, clip, skirt=P.get('skirt', False))
        ps = [f / T for f in range(T)]
        for p in ps: body_pose(J, P, clip, p, extra)
        raw = [extra['zraw'][p] for p in ps]
        sm = raw[:]
        for _ in range(1):   # circular smoothing, never above the reach limit
            sm = [min(raw[i], (sm[i - 1] + 2 * sm[i] + sm[(i + 1) % T]) / 4) for i in range(T)]
        extra['zs'] = dict(zip(ps, sm)); extra['miss'] = []
    nm = f"{rig.name.replace('_Rig', '')}_{clip}"
    for old in [x for x in bpy.data.actions if x.name == nm or x.name.startswith(nm + '.')]: bpy.data.actions.remove(old)
    act = bpy.data.actions.new(nm)
    act.use_fake_user = True
    rig.animation_data_create(); rig.animation_data.action = act
    bones = rig.pose.bones
    rest = {b.name: b.bone.matrix_local.to_quaternion() for b in bones}
    prev = {}
    for f in range(T + 1):
        p = (f % T) / T
        R, off = body_pose(J, P, clip, p, extra)
        for pb in bones:
            pb.rotation_mode = 'QUATERNION'
            q = R.get(pb.name, Quaternion())
            r = rest[pb.name]
            b = r.inverted() @ q @ r
            if pb.name in prev and prev[pb.name].dot(b) < 0: b.negate()
            prev[pb.name] = b
            pb.rotation_quaternion = b
            pb.keyframe_insert('rotation_quaternion', frame=f, group=pb.name)
            if pb.name == 'pelvis':
                pb.location = rest['pelvis'].inverted() @ off
                pb.keyframe_insert('location', frame=f, group=pb.name)
    for fc in iter_fcurves(act):
        for kp in fc.keyframe_points: kp.interpolation = 'LINEAR'
    act.frame_range = (0, T); act.use_frame_range = True; act.use_cyclic = True
    miss = max(extra.get('miss', [0.0]) or [0.0])
    return act, miss

def iter_fcurves(act):
    if hasattr(act, 'layers'):
        for ly in act.layers:
            for st in ly.strips:
                for cb in st.channelbags:
                    yield from cb.fcurves
    else:
        yield from act.fcurves

def animate_all(info):
    rep = {}
    for nm, d in info.items():
        rig = d['rig']; acts = []
        for clip in ('Idle', 'Walk', 'Run'):
            act, miss = bake_clip(rig, d['J'], d['P'], clip); acts.append((clip, act))
            rep[f'{nm}_{clip}'] = round(miss, 4)
        ad = rig.animation_data; ad.action = None
        for tr in list(ad.nla_tracks): ad.nla_tracks.remove(tr)
        for clip, act in acts:
            tr = ad.nla_tracks.new(); tr.name = clip
            st = tr.strips.new(clip, 0, act)
            try: st.action_slot = act.slots[0]
            except Exception: pass
            tr.mute = clip != 'Idle'
        for pb in rig.pose.bones:
            pb.rotation_quaternion = Quaternion(); pb.location = V()
    return rep

def play(rig, clip):
    """Assign a clip as the active action (for previews)."""
    ad = rig.animation_data
    act = bpy.data.actions[f"{rig.name.replace('_Rig', '')}_{clip}"]
    for tr in ad.nla_tracks: tr.mute = True
    ad.action = act
    try: ad.action_slot = act.slots[0]
    except Exception: pass
