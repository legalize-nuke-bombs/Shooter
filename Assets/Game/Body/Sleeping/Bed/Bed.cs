using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.Body.Sleeping
{
    public class Bed : MonoBehaviour, IUsable, IDigestible
    {
        private static readonly Journal Log = Logs.Here();

        public UsageType Usage => UsageType.Sleep;

        public void Use(NetworkObject user)
        {
            var sleeper = user.GetComponent<Sleeper>();
            if (sleeper == null) return;

            var health = user.GetComponent<Health>();
            var hands = user.GetComponent<Hands>();

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

        public string Digest(DigestionDetail detail)
        {
            return "Место для сна";
        }

        public DigestionPriority Priority => DigestionPriority.High;
    }
}
