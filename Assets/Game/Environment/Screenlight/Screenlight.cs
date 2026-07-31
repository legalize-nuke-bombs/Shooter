using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Light))]
    public class Screenlight : MonoBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        private static readonly int Emissive = Shader.PropertyToID("_EmissiveColorMap");

        [SerializeField] private Renderer screen;

        [SerializeField] private float brightest = 60f;

        [SerializeField] private float unrest = 0.35f;

        [SerializeField] private float pace = 12f;

        private Light glow;
        private Material picture;
        private float own;

        private void Awake()
        {
            glow = GetComponent<Light>();
            picture = screen == null ? null : screen.material;
            own = Mathf.Abs(transform.position.x * 0.7f + transform.position.z * 1.3f);

            Log.Info("Screen {}: light {} enabled {} intensity {} range {}, renderer {}, material {}, shader {}, mesh {}",
                name, glow.type, glow.enabled, glow.intensity, glow.range,
                screen == null ? "none" : screen.name,
                picture == null ? "none" : picture.name,
                picture == null ? "none" : picture.shader.name,
                screen == null ? "none" : screen.GetComponent<MeshFilter>() == null ? "no filter"
                    : screen.GetComponent<MeshFilter>().sharedMesh == null ? "no mesh"
                    : screen.GetComponent<MeshFilter>().sharedMesh.vertexCount.ToString());
        }

        private void Update()
        {
            float moment = Time.time * pace + own;

            glow.intensity = brightest * Mathf.Max(0f, 1f + unrest * Wave(moment));
            if (picture == null) return;

            var shift = new Vector2(Snap(moment), Snap(moment + 71f));
            picture.SetTextureOffset(Emissive, shift);
        }

        private static float Wave(float moment)
        {
            return Mathf.PerlinNoise(moment, 0.5f) * 2f - 1f;
        }

        private static float Snap(float moment)
        {
            return Mathf.PerlinNoise(Mathf.Floor(moment) * 0.37f, 0.5f);
        }
    }
}
