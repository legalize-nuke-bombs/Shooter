using Newtonsoft.Json.Linq;
using Shooter.Game.Body;
using Shooter.Game.Core;
using Shooter.Game.Core.Saves;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.World
{
    [RequireComponent(typeof(Speaker))]
    public class StructureHealth : MonoBehaviour, IDigestible, ISaveableComponent
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private bool broken;

        [SerializeField] private SoundSpec breakSound;

        private Speaker speaker;
        public bool Broken => broken;

        public string ComponentKey => "StructureHealth";
        struct SaveData
        {
            public bool Broken { get; set; }
        }
        public object SaveObject()
        {
            return new SaveData()
            {
                Broken = broken
            };
        }
        public void LoadObject(SaveToken jToken)
        {
            SaveData sd = jToken.To<SaveData>();
            broken = sd.Broken;
        }

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
                Log.Warn($"Entity {name} can not be broken because it is already broken");
                return;
            }

            Log.Info($"Entity {name} became broken");

            broken = true;

            speaker.Play(breakSound);

            foreach (IBreakable breakable in GetComponents<IBreakable>())
                breakable.Broken();
        }
    }
}
