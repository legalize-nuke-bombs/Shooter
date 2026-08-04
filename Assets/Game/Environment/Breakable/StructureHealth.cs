using Shooter.Game.Sweeping;
using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(AutoSweepable))]
    public class StructureHealth : NetworkBehaviour, ISweepable
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private bool broken = false;
        public bool Broken => broken;

        // TODO Поставить НЕТ по умолчанию
        [SerializeField] private bool useDespawn = true;
        [SerializeField] private float despawnTime = 10f;

        private float timeSinceBroken;

        public void Break()
        {
            Log.Info("Entity {} became broken", name);

            broken = true;
            timeSinceBroken = 0f;

            foreach (IBreakable breakable in GetComponents<IBreakable>())
                breakable.Broken();
        }

        public bool CanBeSwept()
        {
            return broken && useDespawn && (timeSinceBroken >= despawnTime);
        }

        // TODO Автоломание - заглушка, удалить
        private float breakTimer = 10f;
        public void Update()
        {
            if (broken && useDespawn)
            {
                timeSinceBroken += Time.deltaTime;
            }

            if (!broken)
            {
                if (breakTimer <= 0f)
                {
                    Break();
                }
                else
                {
                    breakTimer -= Time.deltaTime;
                }
            }
        }
    }
}
