using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;
using Environment = Shooter.Game.World.Environment;
using Shooter.Game.Core;
using Shooter.Game.World;

namespace Shooter.Game.Body
{
    public class Bed : MonoBehaviour, IUsable, IDigestible
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private Camera bedside;

        public UsageType Usage => UsageType.Sleep;

        public Camera Bedside => bedside != null ? bedside : bedside = GetComponentInChildren<Camera>(true);

        public void Use(NetworkObject user)
        {
            var sleeper = user.GetComponent<Sleeper>();
            if (sleeper == null) return;

            var health = user.GetComponent<Health>();
            var hands = user.GetComponent<Hands>();

            bool alive = health == null || health.Alive;
            bool handsFree = hands == null || hands.Free;
            bool night = Clock.Current != null && Clock.Current.IsNight();

            if (!SleepRule.CanSleep(alive, handsFree, night))
            {
                Log.Info($"Entity {user.name} can not sleep in {this.NameOf()}: alive {alive}, hands free {handsFree}, night {night}");
                return;
            }

            sleeper.FallAsleep(this);
        }

        public string Digest(DigestionDetail detail)
        {
            return "A place to sleep";
        }

        public DigestionPriority Priority => DigestionPriority.High;
    }
}
