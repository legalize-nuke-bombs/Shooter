using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [RequireComponent(typeof(Crafter))]
    [RequireComponent(typeof(Speaker))]
    public class SoundOnCraft : ShooterBehaviour
    {
        private Crafter crafter;
        private Speaker speaker;

        protected override void ShooterAwake()
        {
            crafter = GetComponent<Crafter>();
            speaker = GetComponent<Speaker>();
        }

        private void OnEnable()
        {
            crafter.Crafted += Play;
        }

        private void OnDisable()
        {
            crafter.Crafted -= Play;
        }

        private void Play(CraftAsset craft)
        {
            if (craft.Sound != null) speaker.PlayForEveryone(craft.Sound);
        }
    }
}
