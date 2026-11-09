using UnityEditor;
using UnityEngine;

namespace Zulvyre.EditorTools
{
    public static class ModelReport
    {
        [MenuItem("Zulvyre/Report Model Mesh Data")]
        public static void Report()
        {
            string[] ids = { "house", "longhouse", "spire", "watchtower", "pine", "oak", "rock", "well", "gate", "pillar", "lanternpost", "fence", "crate", "hero", "orc", "vampire", "wolf", "skeleton", "human:man", "human:woman", "human:child" };

            for (int i = 0; i < ids.Length; i++)
            {
                string id = ids[i];
                string path = id.StartsWith("human:")
                    ? "Art3D/Humans/" + id.Substring(6) + "_idle"
                    : "Art3D/" + id;

                var prefab = Resources.Load<GameObject>(path);
                if (prefab == null)
                {
                    Debug.Log("[ModelReport] " + id + " MISSING");
                    continue;
                }

                var renderers = prefab.GetComponentsInChildren<Renderer>();
                int tris = 0, verts = 0, uvVerts = 0, mats = 0;
                string matNames = "";
                bool smooth = false;

                for (int r = 0; r < renderers.Length; r++)
                {
                    var filter = renderers[r] as MeshRenderer != null ? renderers[r].GetComponent<MeshFilter>() : null;
                    var mesh = renderers[r] is SkinnedMeshRenderer sk ? sk.sharedMesh : (filter != null ? filter.sharedMesh : null);
                    if (mesh != null)
                    {
                        tris += mesh.triangles.Length / 3;
                        verts += mesh.vertexCount;
                        if (mesh.uv != null && mesh.uv.Length == mesh.vertexCount) uvVerts += mesh.vertexCount;
                        smooth = smooth || mesh.normals.Length > 0;
                    }

                    var shared = renderers[r].sharedMaterials;
                    mats += shared.Length;
                    for (int m = 0; m < shared.Length; m++)
                    {
                        if (shared[m] == null) continue;
                        matNames += (matNames.Length > 0 ? "," : "") + shared[m].name;
                    }
                }

                Debug.Log($"[ModelReport] {id}: renderers={renderers.Length} mats=[{matNames}] tris={tris} verts={verts} withUV={uvVerts} withNormals={smooth}");
            }
        }
    }
}
