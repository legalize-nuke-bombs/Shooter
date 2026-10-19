using System.Linq;
using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    public class Bed : MonoBehaviour, IUsable, IDigestible
    {
        private static readonly Journal Log = Logs.CreateJournalForThisFile();

        [SerializeField] private Camera bedside;

        private SleepCycle sleepCycle;

        public Camera BedsideCamera => bedside != null ? bedside : bedside = GetComponentInChildren<Camera>(true);

        private void Awake()
        {
            sleepCycle = Registers.GetAll<SleepCycle>(RegisterInactivePolicy.Include).Single();
        }

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

            bool alive = health == null || health.IsAlive;
            bool handsFree = hands == null || hands.AreFree;
            bool bedtime = sleepCycle.IsBedtime();

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
