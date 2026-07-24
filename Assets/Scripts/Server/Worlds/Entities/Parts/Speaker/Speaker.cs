using System.Collections.Generic;

namespace Shooter.Server.Worlds.Entities.Parts.Speaker
{
    public sealed class Speaker : Part
    {
        private const int RecentLimit = 5;

        private readonly Queue<Sound> recent = new Queue<Sound>();
        private long counter;

        public Speaker(Entity self) : base(self, typeof(Speaker))
        {
        }

        public void Play(SoundType soundType)
        {
            recent.Enqueue(new Sound
            {
                Id = counter,
                Type = soundType
            });
            counter++;

            while (recent.Count > RecentLimit)
                recent.Dequeue();
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
