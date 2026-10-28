using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Sleeper))]
    [RequireComponent(typeof(RespawnPosition))]
    public class RespawnPositionOnFallAsleep : MonoBehaviour
    {
        private Sleeper sleeper;
        private RespawnPosition respawnPosition;

        private void Awake()
        {
            sleeper = GetComponent<Sleeper>();
            respawnPosition = GetComponent<RespawnPosition>();
        }

        private void OnEnable()
        {
            sleeper.FellAsleep += RememberSleepingPlace;
        }

        private void OnDisable()
        {
            sleeper.FellAsleep -= RememberSleepingPlace;
        }

        private void RememberSleepingPlace()
        {
            respawnPosition.SetPosition(transform.position);
        }
    }
}
