using Shooter.Game.Body;
using Shooter.Game.Core;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.World
{
    [RequireComponent(typeof(Sweepable))]
    [RequireComponent(typeof(Speaker))]
    public class StructureHealth : MonoBehaviour, ISweepingRule, IDigestible
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
            if (Broken)
            {
                Log.Warn($"Entity {name} can not be broken because it is already broken");
                return;
            }

            Log.Info($"Entity {name} became broken");

            broken = true;
            brokenAt = Time.time;

            speaker.Play(breakSound);

            foreach (IBreakable breakable in GetComponents<IBreakable>())
                breakable.Broken();
        }

        public string Digest(DigestionDetail detail)
        {
            return Broken ? "Broken" : "Intact";
        }

        public DigestionPriority Priority => DigestionPriority.Medium;

        public bool Permits => broken && useDespawn && (Time.time - brokenAt >= despawnTime);
    }
}
