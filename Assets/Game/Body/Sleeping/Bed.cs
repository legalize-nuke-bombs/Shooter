using Shooter.Game.Core;
using Shooter.Game.World;
using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game.Body
{
    public class Bed : MonoBehaviour, IUsable, IDigestible
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private Camera bedside;

        public Camera Bedside => bedside != null ? bedside : bedside = GetComponentInChildren<Camera>(true);

        public string Digest(DigestionDetail detail)
        {
            return "A place to sleep";
        }

        public DigestionPriority Priority => DigestionPriority.High;

        public UsageType Usage => UsageType.Sleep;

        public void Use(NetworkObject user)
        {
            Sleeper sleeper = user.GetComponent<Sleeper>();
            if (sleeper == null) return;

            Health health = user.GetComponent<Health>();
            Hands hands = user.GetComponent<Hands>();

            bool alive = health == null || health.Alive;
            bool handsFree = hands == null || hands.Free;
            bool night = Clock.Current != null && Clock.Current.IsNight();

            if (!SleepRule.CanSleep(alive, handsFree, night))
            {
                Log.Info(
                    $"Entity {user.name} can not sleep in {name}: alive {alive}, hands free {handsFree}, night {night}");
                return;
            }

            sleeper.FallAsleep(this);
        }
    }
}
