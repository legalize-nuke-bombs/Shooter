using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    public class Bed : ShooterBehaviour, IUsable, IDigestible
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private Camera bedside;

        private SleepCycle sleepCycle;

        public Camera BedsideCamera => bedside != null ? bedside : bedside = GetComponentInChildren<Camera>(true);

        protected override void ShooterAwake()
        {
            sleepCycle = Registers.Instance.Get<SleepCycle>();
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

            if (!sleepCycle.IsBedtime())
            {
                Log.Info($"Entity {user.name} can not sleep in {name}: it is not bedtime");
                promptResult = "Failed to sleep: it is not bedtime";
                return;
            }

            if (!sleeper.TryFallAsleep(this, out string refusal))
            {
                Log.Info($"Entity {user.name} can not sleep in {name}: {refusal}");
                promptResult = $"Failed to sleep: {refusal}";
                return;
            }

            promptResult = "You fell asleep";
        }
    }
}
