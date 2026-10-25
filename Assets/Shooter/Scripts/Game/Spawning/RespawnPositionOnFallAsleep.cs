using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Sleeper))]
    [RequireComponent(typeof(RespawnPosition))]
    public class RespawnPositionOnFallAsleep : ShooterBehaviour
    {
        private Sleeper sleeper;
        private RespawnPosition respawnPosition;

        protected override void ShooterAwake()
        {
            sleeper = GetComponent<Sleeper>();
            respawnPosition = GetComponent<RespawnPosition>();
        }

        protected override void ShooterOnEnable()
        {
            sleeper.FellAsleep += RememberSleepingPlace;
        }

        protected override void ShooterOnDisable()
        {
            sleeper.FellAsleep -= RememberSleepingPlace;
        }

        private void RememberSleepingPlace()
        {
            respawnPosition.SetPosition(transform.position);
        }
    }
}
