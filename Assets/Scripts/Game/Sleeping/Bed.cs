using Unity.Netcode;
using UnityEngine;
using Shooter.Game.Interacting;
using Shooter.Logging;

namespace Shooter.Game.Sleeping
{
    public class Bed : MonoBehaviour, IUsable
    {
        public void Use(NetworkObject user)
        {
            var sleeper = user.GetComponent<Sleeper>();
            if (sleeper == null) return;

            var health = user.GetComponent<Health.Health>();
            var hands = user.GetComponent<Hands.Hands>();

            bool alive = health == null || health.Alive;
            bool handsFree = hands == null || hands.Free;
            bool night = Environment.Current != null && Environment.Current.Clock.IsNight();

            if (!SleepRule.CanSleep(alive, handsFree, night))
            {
                Log.Info("Entity {} can not sleep in {}: alive {}, hands free {}, night {}", user.name, name, alive, handsFree, night);
                return;
            }

            sleeper.FallAsleep();
        }
    }
}
