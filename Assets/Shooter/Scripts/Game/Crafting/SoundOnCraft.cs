using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Crafter))]
    [RequireComponent(typeof(Speaker))]
    public class SoundOnCraft : MonoBehaviour
    {
        private Crafter crafter;
        private Speaker speaker;

        private void Awake()
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
            if (craft.Sound != null) speaker.Play(craft.Sound);
        }
    }
}
