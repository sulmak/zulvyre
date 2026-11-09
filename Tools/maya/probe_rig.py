"""Inspect the joint local translations that build_humans.py produces.

Usage:
  mayapy Tools/maya/probe_rig.py
"""
import os
import sys

import maya.standalone

maya.standalone.initialize(name="python")
import maya.cmds as cmds  # noqa: E402

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import build_humans as bh  # noqa: E402

cmds.currentUnit(linear="m", time="film")
table, anchors = bh.joints_for(bh.CHARS["man"])
rig = bh.build_rig(table)

print("RIGPROBE name | desired_world | localTranslate | worldTranslate | jointOrient")
for name in ("root", "hips", "spine", "chest", "neck", "head",
             "clavicle_l", "upperarm_l", "forearm_l", "hand_l",
             "thigh_l", "shin_l", "foot_l", "toe_l"):
    j = rig[name]
    t = cmds.getAttr(j + ".translate")[0]
    w = cmds.xform(j, q=True, ws=True, t=True)
    print("RIGPROBE %s desired=%s local=(%.3f, %.3f, %.3f) world=(%.3f, %.3f, %.3f) orient=(%.1f, %.1f, %.1f)" % (
        name, tuple(round(v, 3) for v in table[name][0]), t[0], t[1], t[2], w[0], w[1], w[2],
        cmds.getAttr(j + ".jointOrientX"), cmds.getAttr(j + ".jointOrientY"), cmds.getAttr(j + ".jointOrientZ")))

maya.standalone.uninitialize()
