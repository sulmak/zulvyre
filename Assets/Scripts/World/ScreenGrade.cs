using UnityEngine;

namespace Zulvyre
{
    /// <summary>
    /// Small full-screen grade (contrast, saturation, warm tint, vignette) so the flat
    /// built-in pipeline light reads with more depth without a post package.
    /// </summary>
    public class ScreenGrade : MonoBehaviour
    {
        static Shader cachedShader;
        Material material;

        public float Contrast = 1.07f;
        public float Saturation = 1.06f;
        public Color Tint = new Color(1.01f, 1f, 0.98f);
        public float Vignette = 0.38f;

        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (material == null)
            {
                if (cachedShader == null)
                {
                    cachedShader = Resources.Load<Shader>("ZulvyreGrade");
                    if (cachedShader == null) cachedShader = Shader.Find("Hidden/Zulvyre/Grade");
                }
                if (cachedShader == null || !cachedShader.isSupported)
                {
                    Graphics.Blit(source, destination);
                    return;
                }
                material = new Material(cachedShader) { hideFlags = HideFlags.HideAndDontSave };
            }

            material.SetFloat("_Contrast", Contrast);
            material.SetFloat("_Saturation", Saturation);
            material.SetColor("_Tint", Tint);
            material.SetFloat("_Vignette", Vignette);
            Graphics.Blit(source, destination, material);
        }

        void OnDestroy()
        {
            if (material != null) Destroy(material);
        }
    }
}
