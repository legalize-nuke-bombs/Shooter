using UnityEngine;
using Shooter.Client.Sounds;
using Shooter.Server.Worlds.Entities;
using Shooter.Server.Worlds.Entities.Parts.Speaker;

namespace Shooter.Client.Worlds.Entities
{
    public sealed class OwnEntityView : EntityView
    {
        private readonly Transform body;
        private readonly SpeakerView speaker;

        public OwnEntityView(EntityState state, Transform body) : base(state)
        {
            this.body = body;
            speaker = new SpeakerView(body.gameObject);
            Apply(state);
        }

        public override void Tick(float dt)
        {
            BodyLerp.Follow(body, Position, dt);
        }

        protected override void OnApply(SpeakerState speakerState)
        {
            speaker.Apply(speakerState);
        }
    }
}
