using UnityEngine;

namespace Shooter.Client.Interface
{
    [ExecuteAlways]
    [RequireComponent(typeof(Light))]
    public class MenuMoon : MonoBehaviour
    {
        private const float Opposite = 180f;

        [SerializeField] private Camera view;
        [SerializeField] [Range(-10f, 89f)] private float elevation = 20f;
        [SerializeField] [Range(-180f, 180f)] private float sideways;

        private void Update()
        {
            Aim();
        }

        private void OnValidate()
        {
            Aim();
        }

        private void Aim()
        {
            if (view == null) return;

            float yaw = view.transform.eulerAngles.y + Opposite + sideways;
            transform.rotation = Quaternion.Euler(elevation, yaw, 0f);
        }
    }
}
