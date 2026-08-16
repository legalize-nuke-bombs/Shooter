using Shooter.Client.Playing;
using Shooter.Game.Body;
using UnityEngine;

namespace Shooter.Client.Perception
{
    public sealed class CameraSway : PerceptionEffect
    {
        private const float NodShare = 0.4f;
        private const float NodFrequencyShare = 0.7f;

        private readonly CameraSwaySpec spec;
        private readonly IntoxicationView view;
        private float felt;

        public CameraSway(CameraSwaySpec spec)
        {
            this.spec = spec;
            view = OwnPlayer.Find<IntoxicationView>();
        }

        public override void Tick(float strength)
        {
            felt = Mathf.MoveTowards(felt, strength, Time.deltaTime * spec.SettleSpeed);

            float phase = Time.time * spec.SwayFrequency * Mathf.PI * 2f;
            float roll = Mathf.Sin(phase) * spec.SwayDegrees * felt;
            float nod = Mathf.Sin(phase * NodFrequencyShare) * spec.SwayDegrees * NodShare * felt;

            if (view != null) view.CameraSway += new Vector3(nod, 0f, roll);
        }

        public override void End()
        {
        }
    }
}
