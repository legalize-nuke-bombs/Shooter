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

        public void Use(NetworkObject user, out string promptResult)
        {
            Sleeper sleeper = user.GetComponent<Sleeper>();
            if (sleeper == null)
            {
                promptResult = "Failed to sleep: your character does not sleep";
                return;
            }

            Health health = user.GetComponent<Health>();
            Hands hands = user.GetComponent<Hands>();

            bool alive = health == null || health.Alive;
            bool handsFree = hands == null || hands.Free;
            bool bedtime = GameState.Get<SleepCycle>() != null && GameState.Get<Clock>() != null && GameState.Get<SleepCycle>().IsBedtime();

            if (!SleepRule.CanSleep(alive, handsFree, bedtime))
            {
                Log.Info($"Entity {user.name} can not sleep in {name}: alive {alive}, hands free {handsFree}, bedtime {bedtime}");
                promptResult = $"Failed to sleep: alive {alive} (must be true) hands free {handsFree} (must be true) bedtime {bedtime} (must be true)";
                return;
            }

            sleeper.FallAsleep(this);
            promptResult = "You fell asleep";
        }
    }
}
