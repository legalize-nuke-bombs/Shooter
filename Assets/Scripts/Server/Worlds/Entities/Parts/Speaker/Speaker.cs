using System.Collections.Generic;

namespace Shooter.Server.Worlds.Entities.Parts.Speaker
{
    public class Speaker : Part
    {
        private readonly Queue<Sound> recent = new Queue<Sound>();
        private long ctr = 0;

        public void Play(SoundType soundType)
        {
            var sound = new Sound
            {
                Id = ctr,
                Type = soundType
            };
            recent.Enqueue(sound);

            ctr++;

            while (recent.Count > 5)
            {
                recent.Dequeue();
            }
        }

        public override PartState State()
        {
            return new SpeakerState
            {
                Recent = recent.ToArray()
            };
        }
    }
}
