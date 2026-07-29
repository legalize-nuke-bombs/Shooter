using Shooter.Logging;
using UnityEngine;

namespace Shooter.Client.Interface.Dreaming
{
    [CreateAssetMenu(menuName = "Shooter/Dream Catalog", fileName = "DreamCatalog")]
    public sealed class DreamCatalog : ScriptableObject
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private DreamSpec[] dreams;

        public DreamSpec Pick()
        {
            float total = 0f;
            foreach (DreamSpec dream in dreams)
            {
                if (dream != null) total += dream.Weight;
            }

            if (total <= 0f)
            {
                Log.Warn("Dream catalog {} has nothing to dream", name);
                return null;
            }

            float roll = Random.value * total;
            DreamSpec last = null;

            foreach (DreamSpec dream in dreams)
            {
                if (dream == null) continue;

                last = dream;
                roll -= dream.Weight;

                if (roll <= 0f) return dream;
            }

            return last;
        }
    }
}
