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

            MeshFilter filter = screen == null ? null : screen.GetComponent<MeshFilter>();
            string mesh = screen == null ? "none" : filter == null ? "no filter" : filter.sharedMesh == null ? "no mesh" : filter.sharedMesh.vertexCount.ToString();

            Log.Info($"Screen {name}: light {glow.type} enabled {glow.enabled} intensity {glow.intensity} range {glow.range}, renderer {(screen == null ? "none" : screen.name)}, material {(picture == null ? "none" : picture.name)}, shader {(picture == null ? "none" : picture.shader.name)}, mesh {mesh}");
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
