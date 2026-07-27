using UnityEngine;

namespace Shooter.Game.Lighting
{
    [RequireComponent(typeof(Light))]
    public class Campfire : MonoBehaviour
    {
        [SerializeField] private float lumens = 2000f;

        [SerializeField] private float flickerDepth = 0.25f;

        [SerializeField] private float flickerSpeed = 6f;

        private Light glow;

        private float phase;

        private void Awake()
        {
            glow = GetComponent<Light>();
            phase = Random.value * 1000f;
        }

        private void Update()
        {
            float wobble = Mathf.PerlinNoise(phase + Time.time * flickerSpeed, 0f) * 2f - 1f;

            glow.intensity = lumens * (1f + wobble * flickerDepth);
        }
    }
}
