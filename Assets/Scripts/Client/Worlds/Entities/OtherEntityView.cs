using UnityEngine;
using Shooter.Client.Sounds;
using Shooter.Server.Worlds.Entities;
using Shooter.Server.Worlds.Entities.Parts.Speaker;

namespace Shooter.Client.Worlds.Entities
{
    public sealed class OtherEntityView : EntityView
    {
        private static readonly Color PilotedColor = new Color(0.9f, 0.4f, 0.3f);
        private static readonly Color NpcColor = new Color(0.5f, 0.55f, 0.5f);

        private readonly Transform body;
        private readonly SpeakerView speaker;

        public OtherEntityView(EntityState state) : base(state)
        {
            GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = "Entity_" + state.Id;
            capsule.transform.position = Position;
            ClientEntityBody.Attach(capsule, this);

            body = capsule.transform;
            speaker = new SpeakerView(capsule);

            Apply(state);
            capsule.GetComponent<Renderer>().material.color = Piloted ? PilotedColor : NpcColor;
        }

        public override void Tick(float dt)
        {
            Quaternion rotation = Quaternion.Euler(0f, Yaw, 0f);
            if (Sleeping || !Alive) rotation *= Quaternion.Euler(0f, 0f, 90f);

            BodyLerp.Follow(body, Position, rotation, dt);
        }

        public override void Destroy()
        {
            Object.Destroy(body.gameObject);
        }

        protected override void OnApply(SpeakerState speakerState)
        {
            speaker.Apply(speakerState);
        }
    }
}
