"""Vivi v3: natural finger curls + a skirt bone ring with baked secondary motion (exec'd after lp_vivi2.py).

Skirt: K panels around the hips, each two bones (upper/lower). Per frame the panels follow the hips, are pushed
out by the legs (capsule collision in the panel plane), lift with vertical acceleration and trail back with
forward speed, through a damped spring. Everything is baked into the Idle/Walk/Run actions, so Unity just plays it.
"""
SKIRT_K = 12
SKIRT_CLEAR = {'Idle': 0.02, 'Walk': 0.035, 'Run': 0.045}   # m between leg surface and skirt, per clip
SKIRT_CLEAR_DEFAULT = 0.035                     # m between leg surface and skirt
SKIRT_SPRING_HZ, SKIRT_DAMP = 3.0, 0.38
SKIRT_SPREAD = (1.0, 0.72, 0.38)   # share of a panel's swing given to neighbours 1..4 away
SKIRT_DRAG = {'Idle': 0.0, 'Walk': 0.05, 'Run': 0.12}      # rad the back panels trail by at that speed
SKIRT_BOUNCE = 0.008                     # rad per m/s^2 of downward hip acceleration
FINGERS = ('Index', 'Middle', 'Ring', 'Pinky')
CURL = {  # MCP, PIP, DIP degrees; thumb 1..3
    'Idle': {'Index': (12, 22, 14), 'Middle': (16, 26, 16), 'Ring': (19, 28, 17), 'Pinky': (23, 30, 18), 'Thumb': (8, 12, 10)},
    'Walk': {'Index': (15, 25, 15), 'Middle': (19, 29, 17), 'Ring': (22, 31, 18), 'Pinky': (26, 33, 19), 'Thumb': (10, 14, 10)},
    'Run':  {'Index': (42, 55, 30), 'Middle': (47, 60, 32), 'Ring': (52, 62, 34), 'Pinky': (56, 62, 35), 'Thumb': (22, 26, 18)},
}

def _rest_c(F, arm, name, tail=False):
    b = arm.data.bones[name]; return F.c(arm.matrix_world @ (b.tail_local if tail else b.head_local))

def finger_curls(F, arm):
    """Per clip: {bone: char-frame rotation relative to parent (rest axes)} curling every finger toward the palm."""
    out = {}
    for clip, table in CURL.items():
        d = {}
        for sd, mx in (('L', 'Left'), ('R', 'Right')):
            pre = f'mixamorig:{mx}Hand'
            idx0 = _rest_c(F, arm, pre + 'Index1'); pin0 = _rest_c(F, arm, pre + 'Pinky1')
            mid0 = _rest_c(F, arm, pre + 'Middle1'); mid1 = _rest_c(F, arm, pre + 'Middle2')
            fdir = (mid1 - mid0).normalized()
            n = (pin0 - idx0).cross(fdir).normalized() * (1 if sd == 'L' else -1)
            if n.z > 0: n = -n                      # T-pose: palms face down
            for fg in FINGERS:
                for seg, ang in zip((1, 2, 3), table[fg]):
                    bn = f'{pre}{fg}{seg}'
                    if bn not in arm.data.bones: continue
                    dd = (_rest_c(F, arm, bn, True) - _rest_c(F, arm, bn)).normalized()
                    ax = dd.cross(n).normalized()
                    d[bn] = Quaternion(ax, math.radians(ang))
            t0 = _rest_c(F, arm, pre + 'Thumb1')
            target = (n * 0.8 + (mid0 - t0).normalized() * 0.6).normalized()
            for seg, ang in zip((1, 2, 3), table['Thumb']):
                bn = f'{pre}Thumb{seg}'
                if bn not in arm.data.bones: continue
                dd = (_rest_c(F, arm, bn, True) - _rest_c(F, arm, bn)).normalized()
                ax = dd.cross(target)
                if ax.length < 1e-4: continue
                d[bn] = Quaternion(ax.normalized(), math.radians(ang))
        out[clip] = d
    return out

# ------------------------------------------------------------------ skirt rig
def add_skirt_rig(info, F, J):
    arm, body = info['arm'], info['body']
    me = body.data; M = body.matrix_world
    bot = {i for i, m in enumerate(me.materials) if m and 'Bottoms' in m.name}
    S = sorted({v for p in me.polygons if p.material_index in bot for v in p.vertices if (M @ me.vertices[v].co).z > 2.5})
    C = {i: F.c(M @ me.vertices[i].co) for i in S}
    cx = sum(p.x for p in C.values()) / len(C); cy = sum(p.y for p in C.values()) / len(C)
    z_hem = min(p.z for p in C.values()); z_piv = J['pelvis'][0].z - 0.01
    K = SKIRT_K; step = 2 * math.pi / K
    angs = [-math.pi / 2 + step * k for k in range(K)]
    def ang_of(p): return math.atan2(p.y - cy, p.x - cx)
    def sector_r(z0, dz, a0):
        rs = [math.hypot(p.x - cx, p.y - cy) for p in C.values() if abs(p.z - z0) < dz and abs((ang_of(p) - a0 + math.pi) % (2 * math.pi) - math.pi) < step * 0.6]
        return max(rs) if rs else None
    panels = []
    for k, a in enumerate(angs):
        rp = sector_r(z_piv, 0.02, a) or 0.15; rh = sector_r(z_hem + 0.02, 0.03, a) or rp * 1.2
        dvec = V((math.cos(a), math.sin(a), 0))
        head = V((cx, cy, z_piv)) + dvec * rp; hem = V((cx, cy, z_hem)) + dvec * rh
        panels.append(dict(k=k, a=a, d=dvec, head=head, mid=head.lerp(hem, 0.5), hem=hem, rp=rp, rh=rh))
    # bones
    vl = bpy.context.view_layer
    for o in vl.objects:
        if o: o.select_set(False)
    arm.select_set(True); vl.objects.active = arm
    bpy.ops.object.mode_set(mode='EDIT')
    eb = arm.data.edit_bones; Ai = arm.matrix_world.inverted()
    hips = eb['mixamorig:Hips']
    for pn in panels:
        b1 = eb.new(f"Skirt_{pn['k']}_1"); b1.head = Ai @ F.w(pn['head']); b1.tail = Ai @ F.w(pn['mid']); b1.parent = hips
        b2 = eb.new(f"Skirt_{pn['k']}_2"); b2.head = b1.tail; b2.tail = Ai @ F.w(pn['hem']); b2.parent = b1; b2.use_connect = True
        b1.use_deform = b2.use_deform = True
    bpy.ops.object.mode_set(mode='OBJECT')
    # weights
    groups = {g.name: g for g in body.vertex_groups}
    for pn in panels:
        for s in (1, 2):
            n = f"Skirt_{pn['k']}_{s}"
            if n not in groups: groups[n] = body.vertex_groups.new(name=n)
    names = {g.index: g.name for g in body.vertex_groups}
    L = z_piv - z_hem
    for i in S:
        p = C[i]; f = (z_piv - p.z) / L
        ws = ss(-0.12, 0.03, f)
        if ws < 1e-3: continue
        s2 = ss(0.4, 0.62, f)
        u = ((ang_of(p) - angs[0]) / step) % K; k0 = int(math.floor(u)) % K; k1 = (k0 + 1) % K; t = u - math.floor(u)
        w = {nm: wt * (1 - ws) for nm, wt in ((names[g.group], g.weight) for g in me.vertices[i].groups)}
        for kk, wa in ((k0, 1 - t), (k1, t)):
            for seg, wb in ((1, 1 - s2), (2, s2)):
                if wa * wb > 1e-4: w[f'Skirt_{kk}_{seg}'] = w.get(f'Skirt_{kk}_{seg}', 0) + ws * wa * wb
        w = dict(sorted(((n, x) for n, x in w.items() if x > 0.01), key=lambda kv: -kv[1])[:4]); tot = sum(w.values())
        for g in list(me.vertices[i].groups): body.vertex_groups[g.group].remove([i])
        for n, x in w.items(): groups[n].add([i], x / tot, 'REPLACE')
    for pn in panels:
        pn['L1'] = (pn['mid'] - pn['head']).length; pn['L2'] = (pn['hem'] - pn['mid']).length
        pn['phi0'] = math.atan2(pn['rh'] - pn['rp'], z_piv - z_hem)
        pn['halfw'] = math.pi * pn['rp'] / K
        # pleat depth: how far the inner folds sit inside the outer ridge line, per height band in this sector
        deps = []
        for zb in [z_hem + (z_piv - z_hem) * q for q in (0.15, 0.3, 0.45, 0.6, 0.75)]:
            rs = [math.hypot(p.x - cx, p.y - cy) for p in C.values() if abs(p.z - zb) < 0.03 and abs((ang_of(p) - pn['a'] + math.pi) % (2 * math.pi) - math.pi) < step * 0.6]
            if len(rs) > 2: deps.append(max(rs) - min(rs))
        pn['inset'] = min(0.06, sorted(deps)[len(deps) // 2]) if deps else 0.03
    return dict(panels=panels, center=(cx, cy), z_piv=z_piv, z_hem=z_hem)

# ------------------------------------------------------------------ secondary motion
def _req_angle(Pp, dv, down, tv, phi_base, Lseg, samples, halfw, inset=0.0, clr=None):
    clr = SKIRT_CLEAR_DEFAULT if clr is None else clr
    """Extra outward angle needed so a panel segment (pivot Pp, rest angle phi_base from `down` toward `dv`) clears all leg capsules."""
    need = 0.0
    for Q, r in samples:
        rel = Q - Pp; u = rel.dot(dv); v = rel.dot(down); w = rel.dot(tv)
        # full requirement for any leg that overlaps this panel's skin (which reaches to the neighbouring panel centres)
        fall = ss(2 * halfw + r + 0.02, 2 * halfw + r, abs(w))
        if fall <= 0 or v < 0.08: continue        # near the waist the skirt hugs the hips by weighting; no leverage there
        rho = math.hypot(u, v)
        if rho > Lseg + r or rho < 1e-4: continue
        phi = math.atan2(u, v) + math.asin(min(1.0, (r + clr + 1.5 * inset) / rho))
        need = max(need, (phi - phi_base) * fall)
    return need

def skirt_secondary(info, F, rig):
    arm = info['arm']; ad = arm.animation_data; sc = bpy.context.scene
    Aw = arm.matrix_world; Ai = Aw.inverted(); A3 = Ai.to_3x3()
    hips_pb = arm.pose.bones['mixamorig:Hips']; Mh0 = arm.data.bones['mixamorig:Hips'].matrix_local
    B = {b.name: b.matrix_local.copy() for b in arm.data.bones if b.name.startswith('Skirt_')}
    panels = rig['panels']; dt = 1 / 30
    k_spring = (2 * math.pi * SKIRT_SPRING_HZ) ** 2; c_damp = 2 * SKIRT_DAMP * math.sqrt(k_spring)
    for tr in ad.nla_tracks: tr.mute = True
    # rest-pose baseline: the original skirt is clean at rest, so only demands beyond these count
    rest_legs = []
    for mx in ('Left', 'Right'):
        bH, bK, bA = (F.c(Aw @ arm.data.bones[f'mixamorig:{mx}{n}'].head_local) for n in ('UpLeg', 'Leg', 'Foot'))
        for s_, r_ in ((0.35, 0.08), (0.55, 0.08), (0.75, 0.07), (0.9, 0.062), (1.0, 0.055)): rest_legs.append((bH.lerp(bK, s_), r_))
        for s_, r_ in ((0.12, 0.064), (0.25, 0.08)): rest_legs.append((bK.lerp(bA, s_), r_))
    dn0 = V((0, 0, -1))
    report = {}
    for clip in ('Idle', 'Walk', 'Run'):
        clr = SKIRT_CLEAR[clip]
        for pn in panels:
            tv0 = dn0.cross(pn['d'])
            pn['base1'] = _req_angle(pn['head'], pn['d'], dn0, tv0, pn['phi0'], pn['L1'] + pn['L2'], rest_legs, pn['halfw'], pn['inset'], clr)
            pn['base2'] = _req_angle(pn['mid'], pn['d'], dn0, tv0, pn['phi0'], pn['L2'], rest_legs, pn['halfw'], pn['inset'], clr)
        act = bpy.data.actions[f'Vivi_{clip}']; ad.action = act
        try: ad.action_slot = act.slots[0]
        except Exception: pass
        T = CLIPS[clip]['T']; frames = []
        for f in range(T):
            sc.frame_set(f)
            D = Aw @ hips_pb.matrix @ Mh0.inverted() @ Ai          # world-space hips delta
            def xp(p): return F.c(D @ F.w(p))
            def xd(v): return (xp(v) - xp(V((0, 0, 0)))).normalized()
            legs = []
            for mx in ('Left', 'Right'):
                H = F.c(Aw @ arm.pose.bones[f'mixamorig:{mx}UpLeg'].head); Kn = F.c(Aw @ arm.pose.bones[f'mixamorig:{mx}Leg'].head)
                An = F.c(Aw @ arm.pose.bones[f'mixamorig:{mx}Foot'].head)
                for s, r in ((0.35, 0.08), (0.55, 0.08), (0.75, 0.07), (0.9, 0.062), (1.0, 0.055)): legs.append((H.lerp(Kn, s), r))   # measured + skin margin
                for s, r in ((0.12, 0.064), (0.25, 0.08)): legs.append((Kn.lerp(An, s), r))
            hz = xp(V((0, 0, rig['z_piv']))).z
            frames.append(dict(D=D, xp=xp, xd=xd, legs=legs, hz=hz))
        # per-frame collision targets
        req1 = [[0.0] * T for _ in panels]
        for f, fr in enumerate(frames):
            down = fr['xd'](V((0, 0, -1)))
            for i, pn in enumerate(panels):
                dv = fr['xd'](pn['d']); dv = (dv - down * dv.dot(down)).normalized(); tv = down.cross(dv)
                req1[i][f] = max(0.0, _req_angle(fr['xp'](pn['head']), dv, down, tv, pn['phi0'], pn['L1'] + pn['L2'], fr['legs'], pn['halfw'], pn['inset'], clr) - pn['base1'])
        acc = [(frames[(f + 1) % T]['hz'] - 2 * frames[f]['hz'] + frames[f - 1]['hz']) / dt ** 2 for f in range(T)]
        bounce = [max(-0.12, min(0.3, -a * SKIRT_BOUNCE)) for a in acc]
        th1 = [[0.0] * T for _ in panels]; vel1 = [[0.0] * T for _ in panels]
        for i, pn in enumerate(panels):
            drag = SKIRT_DRAG[clip] * max(0.0, pn['d'].y)          # +Y = behind her (she runs toward -Y)
            x = v = 0.0
            for lp in range(5):                                    # run to a periodic steady state, keep the last loop
                for f in range(T):
                    tgt = req1[i][f] + drag + bounce[f] * (0.6 + 0.4 * max(0.0, pn['d'].y))
                    a = k_spring * (tgt - x) - c_damp * v
                    v += a * dt; x += v * dt
                    if x < req1[i][f]: x = req1[i][f]; v = max(v, 0.0)
                    x = min(x, 1.25)
                    if lp == 4: th1[i][f] = x; vel1[i][f] = v
        # neighbouring panels share skin: spread each panel's swing around the ring so the hem never shears into spikes
        Kp = len(panels)
        def spread(vals):
            out = []
            for i in range(Kp):
                m = vals[i]
                for j in range(Kp):
                    dk = min((i - j) % Kp, (j - i) % Kp)
                    if 0 < dk < len(SKIRT_SPREAD): m = max(m, vals[j] * SKIRT_SPREAD[dk])
                out.append(m)
            return out
        for f in range(T):
            col = spread([th1[i][f] for i in range(Kp)])
            for i in range(Kp): th1[i][f] = col[i]
        # pass 2: pose + key
        maxa = 0.0
        for f in range(T + 1):
            ff = f % T; sc.frame_set(f); fr = frames[ff]
            down = fr['xd'](V((0, 0, -1)))
            geo, th2s = [], []
            for i, pn in enumerate(panels):
                dv = fr['xd'](pn['d']); dv = (dv - down * dv.dot(down)).normalized(); tv = down.cross(dv)
                axis_a = (A3 @ (F.R @ down.cross(dv))).normalized()
                # lower segment: clears the legs from the bent mid point, lags the upper one's swing
                phi1 = pn['phi0'] + th1[i][ff]
                Pm = fr['xp'](pn['head']) + (down * math.cos(phi1) + dv * math.sin(phi1)) * pn['L1']
                r2 = max(0.0, _req_angle(Pm, dv, down, tv, phi1, pn['L2'], fr['legs'], pn['halfw'], pn['inset'], clr) - pn['base2'])
                th2s.append(max(r2, max(-0.25, min(0.45, -0.035 * vel1[i][ff]))))
                geo.append(axis_a)
                maxa = max(maxa, th1[i][ff])
            th2s = spread(th2s)
            sets2 = []
            for i, pn in enumerate(panels):
                n1 = f"Skirt_{pn['k']}_1"; n2 = f"Skirt_{pn['k']}_2"
                M1f = hips_pb.matrix @ Mh0.inverted() @ B[n1]
                h1 = M1f.translation.copy()
                M1 = Matrix.Translation(h1) @ Matrix.Rotation(th1[i][ff], 4, geo[i]) @ Matrix.Translation(-h1) @ M1f
                arm.pose.bones[n1].matrix = M1
                sets2.append((n1, n2, M1, geo[i], th2s[i]))
            bpy.context.view_layer.update()
            for n1, n2, M1, axis_a, th2 in sets2:
                M2f = M1 @ B[n1].inverted() @ B[n2]; h2 = M2f.translation.copy()
                arm.pose.bones[n2].matrix = Matrix.Translation(h2) @ Matrix.Rotation(th2, 4, axis_a) @ Matrix.Translation(-h2) @ M2f
            bpy.context.view_layer.update()
            for n1, n2, _, _, _ in sets2:
                for n in (n1, n2):
                    pb = arm.pose.bones[n]; pb.rotation_mode = 'QUATERNION'
                    pb.keyframe_insert('rotation_quaternion', frame=f, group=n)
                    pb.keyframe_insert('location', frame=f, group=n)
        for fc in iter_fcurves(act):
            for kp in fc.keyframe_points: kp.interpolation = 'LINEAR'
        report[clip] = round(math.degrees(maxa), 1)
    ad.action = None
    for tr in ad.nla_tracks: tr.mute = tr.name != 'Idle'
    for pb in arm.pose.bones: pb.rotation_quaternion = (1, 0, 0, 0); pb.location = (0, 0, 0)
    return report

def build_vivi_v3():
    G_ = globals()
    G_['STRAIGHT'], G_['A_LINE'], G_['CLEAR'] = 0.0, 0.0, 0.06
    info = build_vivi()
    arm, body = info['arm'], info['body']
    F = Frame(arm); J = vivi_joints(F); P = vivi_params(J)
    rep = dict(skirt_hem=info['hem_after'])
    rep['vest_px'], rep['trim_px'] = recolor_vest(body)
    rep['bow_px'] = recolor_bow(body, F)
    rep['hair_textures'] = recolor_hair(info['objs'])
    G_['FINGER_CURLS'] = finger_curls(F, arm)
    rep['finger_bones'] = len(G_['FINGER_CURLS']['Run'])
    rig = add_skirt_rig(info, F, J)
    rep['actions'] = bake_vivi(info, F, J, P)
    rep['skirt_max_swing_deg'] = skirt_secondary(info, F, rig)
    rep['pleat_inset_m'] = [round(p['inset'], 3) for p in rig['panels']]
    info.update(F=F, J=J, P=P, skirt=rig)
    return info, rep
