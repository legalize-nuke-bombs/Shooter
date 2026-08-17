using Shooter.Game.Body;
using Shooter.Game.Core;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.World
{
    [RequireComponent(typeof(Speaker))]
    public class StructureHealth : MonoBehaviour, IDigestible
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private bool broken;

        [SerializeField] private SoundSpec breakSound;

        private Speaker speaker;
        public bool Broken => broken;

        private void Awake()
        {
            speaker = GetComponent<Speaker>();
        }

        public string Digest(DigestionDetail detail)
        {
            return Broken ? "Broken" : "Intact";
        }

        public DigestionPriority Priority => DigestionPriority.Medium;

        public void Break()
        {
            if (Broken)
            {
                Log.Warn($"Entity {this.NameOf()} can not be broken because it is already broken");
                return;
            }

            Log.Info($"Entity {this.NameOf()} became broken");

            broken = true;

            speaker.Play(breakSound);

            foreach (IBreakable breakable in GetComponents<IBreakable>())
                breakable.Broken();
        }
    }
}
