using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Crafter))]
    [RequireComponent(typeof(Speaker))]
    public class SoundOnCraft : ShooterComponent
    {
        private Crafter crafter;
        private Speaker speaker;

        protected override void ShooterAwake()
        {
            crafter = GetComponent<Crafter>();
            speaker = GetComponent<Speaker>();
        }

        protected override void ShooterOnEnable()
        {
            crafter.Crafted += Play;
        }

        protected override void ShooterOnDisable()
        {
            crafter.Crafted -= Play;
        }

        private void Play(CraftAsset craft)
        {
            if (craft.Sound != null) speaker.Play(craft.Sound);
        }
    }
}
