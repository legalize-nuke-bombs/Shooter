using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Light))]
    public class Firelight : MonoBehaviour
    {
        [SerializeField] private float brightest = 3000f;

        [SerializeField] private float unrest = 0.3f;

        [SerializeField] private float pace = 5f;

        [SerializeField] private float sway = 0.05f;

        private Light flame;
        private Vector3 hearth;
        private float own;

        private void Awake()
        {
            flame = GetComponent<Light>();
            hearth = transform.localPosition;
            own = Mathf.Abs(transform.position.x * 0.7f + transform.position.z * 1.3f);
        }

        private void Update()
        {
            float moment = Time.time * pace + own;

            flame.intensity = brightest * Mathf.Max(0f, 1f + unrest * Wave(moment));
            transform.localPosition = hearth + sway * new Vector3(
                Wave(moment + 13f), Wave(moment + 29f) * 0.5f, Wave(moment + 47f));
        }

        private static float Wave(float moment)
        {
            return Mathf.PerlinNoise(moment, 0.5f) * 2f - 1f;
        }
    }
}
