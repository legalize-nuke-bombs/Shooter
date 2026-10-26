using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Light))]
    public class MainMenuMoon : ShooterBehaviour, IShooterUpdateable
    {
        private const float HalfTurnDegrees = 180f;

        [SerializeField] private Camera view;
        [SerializeField] [Range(-10f, 89f)] private float elevation = 20f;
        [SerializeField] [Range(-180f, 180f)] private float sideways;

        public void ShooterUpdate()
        {
            RotateOppositeToView();
        }

        private void OnValidate()
        {
            RotateOppositeToView();
        }

        private void RotateOppositeToView()
        {
            if (view == null) return;

            float yaw = view.transform.eulerAngles.y + HalfTurnDegrees + sideways;
            transform.rotation = Quaternion.Euler(elevation, yaw, 0f);
        }
    }
}
