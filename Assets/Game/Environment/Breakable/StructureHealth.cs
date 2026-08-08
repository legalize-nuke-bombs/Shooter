using Shooter.Game.Body.Sounding;
using Shooter.Game.Sweeping;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Sweepable))]
    [RequireComponent(typeof(Speaker))]
    public class StructureHealth : MonoBehaviour, ISweepingRule
    {
        private static readonly Journal Log = Logs.Here();

        private Speaker speaker;

        [SerializeField] private bool broken = false;
        public bool Broken => broken;

        [SerializeField] private SoundSpec breakSound = null;

        [SerializeField] private bool useDespawn = false;
        [SerializeField] private float despawnTime = 10f * 60;

        private float brokenAt;

        private void Awake()
        {
            speaker = GetComponent<Speaker>();
        }

        public void Break()
        {
            Log.Info("Entity {} became broken", name);

            broken = true;
            brokenAt = Time.time;

            speaker.Play(breakSound);

            foreach (IBreakable breakable in GetComponents<IBreakable>())
                breakable.Broken();
        }

        public bool Permits => broken && useDespawn && (Time.time - brokenAt >= despawnTime);
    }
}
