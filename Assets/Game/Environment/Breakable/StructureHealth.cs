using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;

namespace Shooter.Game
{
    public class StructureHealth : NetworkBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private bool broken = false;

        public bool Broken => broken;

        public void Break()
        {
            Log.Info("Entity {} became broken", name);

            broken = true;

            foreach (IBreakable breakable in GetComponents<IBreakable>())
                breakable.Broken();
        }

        // TODO Заглушка, удалить
        private float timer = 10f;
        public void Update()
        {
            if (broken)
            {
                return;
            }

            if (timer <= 0f)
            {
                Break();
            }
            else
            {
                timer -= Time.deltaTime;
            }
        }
    }
}
