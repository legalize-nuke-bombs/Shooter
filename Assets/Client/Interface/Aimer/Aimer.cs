using UnityEngine;

namespace Shooter.Client.Interface
{
    public sealed class Aimer : MonoBehaviour
    {
        [SerializeField] private float reach = 10f;

        private int aimedAt = -1;
        private bool hits;
        private RaycastHit hit;

        public float Reach => reach;

        public bool TryHit(out RaycastHit found)
        {
            Cast();

            found = hit;
            return hits;
        }

        private void Cast()
        {
            if (aimedAt == Time.frameCount) return;

            aimedAt = Time.frameCount;
            hits = false;

            Camera view = Camera.main;
            if (view == null) return;

            Transform eyes = view.transform;
            hits = Physics.Raycast(eyes.position, eyes.forward, out hit, reach);
        }
    }
}
