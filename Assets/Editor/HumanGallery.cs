using System.IO;
using UnityEditor;
using UnityEngine;

namespace Zulvyre.EditorTools
{
    public static class HumanGallery
    {
        [MenuItem("Zulvyre/Render Human Gallery")]
        public static void Render()
        {
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Shots", "humans"));
            Directory.CreateDirectory(dir);
            Shots(dir, "man", "idle", 0.4f);
            Shots(dir, "man", "walk", 0.25f);
            Shots(dir, "man", "run", 0.2f);
            Shots(dir, "man", "attack", 0.42f);
            Shots(dir, "man", "die", 1.2f);
            Shots(dir, "woman", "idle", 0.4f);
            Shots(dir, "woman", "walk", 0.25f);
            Shots(dir, "child", "idle", 0.4f);
            Shots(dir, "child", "run", 0.2f);
            Models(dir);
            Debug.Log("[HumanGallery] done -> " + dir);
        }

        static void Shots(string dir, string kind, string clip, float time)
        {
            // Same path the game uses, so grounding and clips are verified as shipped.
            var go = WorldModels.SpawnHuman(kind, null, Vector3.zero, 200f);
            var anim = go.GetComponentInChildren<Animation>();
            var asset = Resources.Load<AnimationClip>("Art3D/Humans/" + kind + "_" + clip);
            if (anim != null && asset != null)
            {
                asset.wrapMode = WrapMode.ClampForever;
                anim.AddClip(asset, clip);
                anim.Play(clip);
                anim[clip].time = Mathf.Clamp(time, 0f, asset.length);
                anim[clip].enabled = true;
                anim.Sample();
            }

            Capture(dir, kind + "-" + clip, go);
            Object.DestroyImmediate(go);
        }

        static void Models(string dir)
        {
            string[] ids = { "hero", "orc", "vampire", "skeleton", "wolf" };
            for (int i = 0; i < ids.Length; i++)
            {
                var go = WorldModels.Spawn(ids[i], null, Vector3.zero, 200f);
                Capture(dir, ids[i], go);
                Object.DestroyImmediate(go);
            }
        }

        static void Capture(string dir, string name, GameObject go)
        {
            string path = Path.Combine(dir, name + ".png");

            Bounds bounds = new Bounds(go.transform.position, Vector3.one);
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            }
            Vector3 center = bounds.center;
            // Frame both axes: long, low models (the wolf) overflow a height-only fit.
            float aspect = 540f / 760f;
            float tanHalf = Mathf.Tan(17f * Mathf.Deg2Rad);
            float fitHeight = bounds.extents.y / tanHalf;
            float fitWidth = bounds.extents.x / (tanHalf * aspect);
            float dist = Mathf.Max(2.4f, Mathf.Max(fitHeight, fitWidth) * 1.35f);

            var camGo = new GameObject("Cam");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.16f, 0.17f, 0.19f);
            cam.fieldOfView = 34f;
            camGo.transform.position = center + new Vector3(0f, 0.08f, -dist);
            camGo.transform.LookAt(center + Vector3.up * 0.04f);

            var keyGo = new GameObject("Key");
            var key = keyGo.AddComponent<Light>();
            key.type = LightType.Directional;
            key.intensity = 1.25f;
            key.color = new Color(1f, 0.94f, 0.86f);
            keyGo.transform.rotation = Quaternion.Euler(32f, 148f, 0f);

            var fillGo = new GameObject("Fill");
            var fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.intensity = 0.35f;
            fill.color = new Color(0.65f, 0.72f, 0.9f);
            fillGo.transform.rotation = Quaternion.Euler(18f, -35f, 0f);

            var floorGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            floorGo.name = "Floor";
            floorGo.transform.position = new Vector3(0f, -0.02f, 0f);
            floorGo.transform.localScale = new Vector3(4f, 0.02f, 4f);
            floorGo.GetComponent<Renderer>().material.color = new Color(0.24f, 0.26f, 0.28f);

            RenderTexture rt = RenderTexture.GetTemporary(540, 760, 24);
            var previous = RenderTexture.active;
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(540, 760, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 540, 760), 0, 0);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);

            Debug.Log("[HumanGallery] " + path + " bounds=" + bounds.center + " " + bounds.size);

            Object.DestroyImmediate(floorGo);
            Object.DestroyImmediate(camGo);
            Object.DestroyImmediate(keyGo);
            Object.DestroyImmediate(fillGo);
            Object.DestroyImmediate(tex);
        }
    }
}
