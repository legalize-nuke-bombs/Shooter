using Shooter.Game.Core;
using UnityEngine;

namespace Shooter.Game.Body
{
    [CreateAssetMenu(menuName = "Shooter/Toxin", fileName = "Toxin")]
    public class ToxinSpec : Spec
    {
        [SerializeField] private float halfLife;
        [SerializeField] private string promptName;

        public float HalfLife => halfLife;
        public string PromptName => promptName;
    }
}
