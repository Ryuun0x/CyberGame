"""v3 hair: scalp cap + broad diamond-section clumps grown from a crown whorl (exec'd after v3_head.py)."""

HAIR_C = None   # projection centre, set in build_hair

def _head_bvh():
    mb = MB()   # head loft only (no ears), for projection
    angs = SEG_A + [180] + [-a for a in reversed(SEG_A[1:])]; n = len(angs)
    rings = [[mb.add(head_point(0, a, r)) for a in angs] for r in HEAD_RINGS]
    for r0, r1 in zip(rings, rings[1:]):
        for i in range(n): mb.face([r0[i], r0[(i + 1) % n], r1[(i + 1) % n], r1[i]], 'Face')
    top = mb.add(V((0, (0.035 + HEAD_Y) * HEAD_H, CHIN_Z + HEAD_H)))
    for i in range(n): mb.face([rings[0][(i + 1) % n], rings[0][i], top], 'Face')
    return mb.bvh()

def cap_off(z):
    """volume of the hair mass above the skull: thick on top, thin at the sides/nape"""
    u = (z - CHIN_Z) / HEAD_H
    return (0.011 + 0.022 * ss(0.45, 0.95, u)) * HK      # puffy anime volume

def proj(bvh, p, extra=0.0):
    """Radially project p from the head centre onto the skull, pushed out by the hair volume + extra."""
    d = (p - HAIR_C).normalized()
    hit, n, _, _ = bvh.ray_cast(HAIR_C, d)
    if hit is None: return p, d
    return hit + d * (cap_off(hit.z) + extra), d

def hairline(a):
    """lowest u of the scalp at azimuth a (deg): forehead, temple, sideburn, behind ear, nape"""
    a = abs(a)
    pts = [(0, 0.68), (35, 0.66), (60, 0.58), (78, 0.44), (92, 0.47), (108, 0.42), (130, 0.28), (155, 0.16), (180, 0.13)]
    for (a0, u0), (a1, u1) in zip(pts, pts[1:]):
        if a0 <= a <= a1: return lerp(u0, u1, (a - a0) / (a1 - a0))
    return pts[-1][1]

def build_cap(bvh):
    mb = MB(); w = {'head': 1.0}
    angs = SEG_A + [180] + [-a for a in reversed(SEG_A[1:])]; n = len(angs); K = 7
    grid = []
    for k in range(K):
        s = k / (K - 1); row = []
        for a in angs:
            u = lerp(hairline(a), 0.965, s ** 0.9)
            p, _ = proj(bvh, head_point(u, a), -0.002 if k == 0 else 0.0)
            row.append(mb.add(p, w))
        grid.append(row)
    for r0, r1 in zip(grid, grid[1:]):
        for i in range(n): mb.face([r0[i], r0[(i + 1) % n], r1[(i + 1) % n], r1[i]], 'Hair_Dark')
    top, _ = proj(bvh, V((0, 0.03 * HEAD_H, CHIN_Z + HEAD_H)))
    t = mb.add(top, w)
    for i in range(n): mb.face([grid[-1][(i + 1) % n], grid[-1][i], t], 'Hair_Dark')
    return mb

def clump(mb, bvh, root, flow, L, W, mat, droop=0.0, sweep=V(), lift=0.008, rise=0.0, steps=8, thick=0.2):
    """Grow one clump: march along `flow` on the hair surface, bending toward gravity/sweep; diamond sections."""
    L, W, lift, rise = L * HK, W * HK, lift * HK, rise * HK
    pts = []; p, _ = proj(bvh, root, -0.004); d = flow.normalized()
    for i in range(steps + 1):
        t = i / steps
        q, nrm = proj(bvh, p, -0.004 + lift * t * t + rise * t)
        pts.append((q, nrm))
        dd = (d + V((0, 0, -1)) * droop * t + sweep * (0.6 + t)).normalized()
        dd = (dd - nrm * dd.dot(nrm)).normalized()           # stay tangent to the head
        p = q + dd * (L / steps); d = dd
    secs = [0, steps // 2, steps]; ws = [W, W * 0.78]
    ring = []
    for si, i in enumerate(secs[:2]):
        q, nrm = pts[i]; tng = (pts[min(i + 1, steps)][0] - pts[max(i - 1, 0)][0]).normalized()
        side = tng.cross(nrm).normalized(); w2 = ws[si] / 2
        th = ws[si] * thick
        ring.append([mb.add(q - side * w2, {'head': 1}), mb.add(q + nrm * th, {'head': 1}),
                     mb.add(q + side * w2, {'head': 1}), mb.add(q - nrm * th * 0.45, {'head': 1})])
    tip = mb.add(pts[-1][0], {'head': 1})
    r0, r1 = ring
    for i in range(4):
        j = (i + 1) % 4
        mb.face([r0[i], r0[j], r1[j], r1[i]], mat)
        mb.face([r1[i], r1[j], tip], mat)
    mb.face([r0[3], r0[2], r0[1], r0[0]], mat)

def build_hair(seed=7):
    """Clean retro mass: strands lie on a rounded volume and flow from the crown; only small crown spikes."""
    global HAIR_C
    HAIR_C = V((0, 0.01 + HEAD_Y * HEAD_H, CHIN_Z + 0.5 * HEAD_H))
    bvh = _head_bvh(); rnd = random.Random(seed)
    mb = build_cap(bvh)
    whorl = head_point(0.93, 165)
    def away(p, extra=V()):
        f = p - whorl; f.z = max(f.z, -0.02); return f + extra
    J = lambda a: rnd.uniform(-a, a)
    # crown layer: broad strands flowing out of the whorl and lying down over the mass
    for i, a in enumerate(range(-180, 180, 40)):
        a2 = a + 15 + J(6); r = head_point(0.9 + J(0.015), a2)
        clump(mb, bvh, r, away(r), 0.11 + J(0.01), 0.08, 'Hair_Light' if i % 3 == 0 else 'Hair', droop=0.6, lift=0.004)
    # top layer: strands rooted right at the top so the crown is covered, fanning out over the crown layer
    for i, a in enumerate(range(-160, 200, 52)):
        r = head_point(0.985, a + J(8))
        d = V((math.sin(math.radians(a)), -math.cos(math.radians(a)), 0.15))
        clump(mb, bvh, r, d, 0.085 + J(0.008), 0.075, 'Hair_Light' if i % 2 else 'Hair', droop=0.35, lift=0.009)
    # spiky tufts poking up out of the top
    for a, u, dx, dy, L in ((20, 0.97, 0.2, -0.5, 0.075), (-40, 0.96, -0.6, -0.2, 0.07), (95, 0.95, 0.8, 0.3, 0.065),
                            (150, 0.95, 0.3, 0.8, 0.07), (-130, 0.955, -0.6, 0.6, 0.068), (-90, 0.94, -0.9, 0.1, 0.06)):
        r = head_point(u, a)
        clump(mb, bvh, r, V((dx * 0.45, dy * 0.45, 1.0)), L, 0.05, 'Hair', droop=0.0, lift=0.02, rise=0.03, thick=0.3)
    # sides and back of the mass: long strands down to the ears / nape
    for k, a in enumerate((66, 92, 118, 144, 170, -156, -130, -104, -78)):
        a2 = a + J(5); r = head_point(0.76 + J(0.015), a2)
        clump(mb, bvh, r, away(r), 0.13 + J(0.01), 0.085, 'Hair_Dark' if k % 3 == 1 else 'Hair', droop=1.3,
              lift=0.016 if abs(a) < 100 else 0.01, thick=0.28)      # puffy; strands near the face flare off the cheeks
    # fringe: broad strands from the front of the crown, close to the forehead, ending at the brows
    for k, (a, L, sw) in enumerate([(44, 0.115, 0.3), (24, 0.125, 0.45), (5, 0.13, 0.55), (-15, 0.125, 0.6),
                                    (-34, 0.115, 0.5), (-52, 0.10, 0.35)]):
        r = head_point(0.9, a)
        clump(mb, bvh, r, V((0, -1, -0.7)), L + 0.012 + J(0.006), 0.085, 'Hair_Light' if k in (1, 4) else 'Hair',
              droop=0.6, sweep=V((-sw * 0.4, 0, 0)), lift=0.012, thick=0.28)
    # side locks in front of the ears, ending on the cheek
    for sg in (1, -1):   # ...standing a little off the cheek, not stuck to it
        for a, u, L in ((78, 0.62, 0.075), (92, 0.64, 0.085)):
            r = head_point(u, sg * a)
            clump(mb, bvh, r, V((sg * 0.25, -0.15, -1)), L + 0.02 + J(0.005), 0.065, 'Hair', droop=0.5, lift=0.018, thick=0.28)
    # over the ears: the ears stay hidden under the hair, as in the reference
    for sg in (1, -1):
        for a, u in ((102, 0.70), (116, 0.68)):
            r = head_point(u, sg * a)
            clump(mb, bvh, r, V((0, 0.1, -1)), 0.115, 0.07, 'Hair', droop=0.6, lift=0.008)
    # nape: a short layer of points
    for k, a in enumerate((128, 152, 176, -160, -136)):
        r = head_point(0.46 + J(0.015), a + J(4))
        clump(mb, bvh, r, V((0, 0.1, -1)), 0.085 + J(0.006), 0.07, 'Hair_Dark' if k % 2 else 'Hair', droop=0.4, lift=0.004)
    return mb
