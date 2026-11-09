using UnityEditor;
using UnityEngine;

namespace Zulvyre.EditorTools
{
    public static class HumanProbe
    {
        [MenuItem("Zulvyre/Probe Humanoid")]
        public static void Probe()
        {
            var prefab = Resources.Load<GameObject>("Art3D/Humans/man_idle");
            if (prefab == null)
            {
                Debug.Log("[Probe] missing prefab");
                return;
            }

            Debug.Log("[Probe] prefab root lpos=" + prefab.transform.localPosition.ToString("F3")
                + " lrot=" + prefab.transform.localEulerAngles.ToString("F1")
                + " scale=" + prefab.transform.localScale.ToString("F3") + " children=" + prefab.transform.childCount);

            var go = Object.Instantiate(prefab);
            var anim = go.GetComponent<Animation>();
            if (anim != null)
            {
                foreach (AnimationState state in anim)
                    Debug.Log("[Probe] clip=" + state.name + " len=" + state.length + " wrap=" + state.wrapMode);
                anim.playAutomatically = false;
                anim.Sample();
            }

            Dump(go.transform, 0);

            var smrs = go.GetComponentsInChildren<SkinnedMeshRenderer>();
            Debug.Log("[Probe] skinned renderers=" + smrs.Length);
            if (smrs.Length > 0)
            {
                var smr = smrs[0];
                Debug.Log("[Probe] first=" + smr.name + " bones=" + smr.bones.Length
                    + " rootBone=" + (smr.rootBone != null ? smr.rootBone.name : "none")
                    + " quality=" + smr.quality + " updateWhenOffscreen=" + smr.updateWhenOffscreen
                    + " vertices=" + smr.sharedMesh.vertexCount
                    + " bindposes=" + smr.sharedMesh.bindposes.Length);
                for (int i = 0; i < smr.bones.Length; i++)
                {
                    var b = smr.bones[i];
                    if (b == null) { Debug.Log("[Probe] bone[" + i + "]=NULL"); continue; }
                    var bp = smr.sharedMesh.bindposes[i];
                    Debug.Log("[Probe] bone[" + i + "] " + b.name
                        + " world=" + b.position.ToString("F3")
                        + " lossy=" + b.lossyScale.ToString("F3")
                        + " bindTrans=" + new Vector3(bp.m03, bp.m13, bp.m23).ToString("F3")
                        + " bindScale=" + new Vector3(bp.m00, bp.m11, bp.m22).ToString("F3"));
                }
            }

            Object.DestroyImmediate(go);
        }

        static void Dump(Transform t, int depth)
        {
            if (depth > 3) return;
            Debug.Log("[Probe] " + new string(' ', depth * 2) + t.name
                + " lpos=" + t.localPosition.ToString("F3")
                + " lrot=" + t.localEulerAngles.ToString("F1")
                + " lscale=" + t.localScale.ToString("F3")
                + " world=" + t.position.ToString("F3")
                + " lossy=" + t.lossyScale.ToString("F3"));
            for (int i = 0; i < t.childCount; i++) Dump(t.GetChild(i), depth + 1);
        }
    }
}
