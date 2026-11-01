using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Light))]
    public class Screenlight : MonoBehaviour, IShooterUpdateable
    {
        private static readonly Journal Log = Logs.Here();

        private static readonly int EmissiveColorMapId = Shader.PropertyToID("_EmissiveColorMap");

        [SerializeField] private Renderer screen;

        [SerializeField] private float brightest = 60f;

        [SerializeField] private float unrest = 0.35f;

        [SerializeField] private float pace = 12f;

        private Light screenLight;
        private float noiseOffset;
        private Material screenMaterial;

        private void Awake()
        {
            screenLight = GetComponent<Light>();
            screenMaterial = screen == null ? null : screen.material;
            noiseOffset = Mathf.Abs(transform.position.x * 0.7f + transform.position.z * 1.3f);

            MeshFilter meshFilter = screen == null ? null : screen.GetComponent<MeshFilter>();
            string meshDescription = screen == null ? "none" :
                meshFilter == null ? "no filter" :
                meshFilter.sharedMesh == null ? "no mesh" : meshFilter.sharedMesh.vertexCount.ToString();

            Log.Info($"Screen {name}: light {screenLight.type} enabled {screenLight.enabled} intensity {screenLight.intensity} range {screenLight.range}, renderer {(screen == null ? "none" : screen.name)}, material {(screenMaterial == null ? "none" : screenMaterial.name)}, shader {(screenMaterial == null ? "none" : screenMaterial.shader.name)}, mesh {meshDescription}");
        }

        public void ShooterUpdate()
        {
            float noiseTime = Time.time * pace + noiseOffset;

            screenLight.intensity = brightest * Mathf.Max(0f, 1f + unrest * GetFlickerNoise(noiseTime));
            if (screenMaterial == null) return;

            Vector2 emissiveOffset = new(GetSteppedNoise(noiseTime), GetSteppedNoise(noiseTime + 71f));
            screenMaterial.SetTextureOffset(EmissiveColorMapId, emissiveOffset);
        }

        private static float GetFlickerNoise(float noiseTime)
        {
            return Mathf.PerlinNoise(noiseTime, 0.5f) * 2f - 1f;
        }

        private static float GetSteppedNoise(float noiseTime)
        {
            return Mathf.PerlinNoise(Mathf.Floor(noiseTime) * 0.37f, 0.5f);
        }
    }
}
