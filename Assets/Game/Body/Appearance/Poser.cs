using UnityEngine;

namespace Shooter.Game.Body
{
    [RequireComponent(typeof(Animator))]
    public class Poser : MonoBehaviour
    {
        private static readonly int SpeedXParameter = Animator.StringToHash("SpeedX");
        private static readonly int SpeedZParameter = Animator.StringToHash("SpeedZ");

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

            if (Time.deltaTime <= 0f) return;

            Vector3 velocity = travel / Time.deltaTime;
            animator.SetFloat(SpeedXParameter, Vector3.Dot(velocity, transform.right), speedDamping, Time.deltaTime);
            animator.SetFloat(SpeedZParameter, Vector3.Dot(velocity, transform.forward), speedDamping, Time.deltaTime);
        }
    }
}
