using UnityEngine;

namespace Shooter.Game.Body.Appearance
{
    [RequireComponent(typeof(Animator))]
    public class Poser : MonoBehaviour
    {
        private static readonly int SpeedParameter = Animator.StringToHash("Speed");

        [SerializeField] private float speedDamping = 0.15f;

        private Animator animator;
        private Vector3 previousPosition;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            previousPosition = transform.position;
        }

        private void Update()
        {
            Vector3 position = transform.position;
            Vector3 travel = position - previousPosition;
            previousPosition = position;
            travel.y = 0f;

            float traveledSpeed = Time.deltaTime > 0f ? travel.magnitude / Time.deltaTime : 0f;
            animator.SetFloat(SpeedParameter, traveledSpeed, speedDamping, Time.deltaTime);
        }
    }
}
