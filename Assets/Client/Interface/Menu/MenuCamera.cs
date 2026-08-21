using UnityEngine;

namespace Shooter.Client.Interface
{
    public class MenuCamera : MonoBehaviour
    {
        private const float FullTurn = Mathf.PI * 2f;
        private const float PitchShare = 0.5f;
        private const float PitchPeriodShare = 0.73f;
        private const float PitchPhase = 1.3f;

        [SerializeField] private float swayDegrees = 1.2f;
        [SerializeField] private float swayPeriod = 19f;
        [SerializeField] private float bobMeters = 0.02f;
        [SerializeField] private float bobPeriod = 7f;

        private Vector3 home;
        private Quaternion rest;

        private void Awake()
        {
            rest = transform.localRotation;
            home = transform.localPosition;
        }

        private void Update()
        {
            float time = Time.time;
            float yaw = Mathf.Sin(time * FullTurn / swayPeriod) * swayDegrees;
            float pitch = Mathf.Sin(time * FullTurn / (swayPeriod * PitchPeriodShare) + PitchPhase) * swayDegrees * PitchShare;
            float bob = Mathf.Sin(time * FullTurn / bobPeriod) * bobMeters;

            transform.localRotation = rest * Quaternion.Euler(pitch, yaw, 0f);
            transform.localPosition = home + Vector3.up * bob;
        }
    }
}
