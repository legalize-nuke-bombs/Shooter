using UnityEngine;

namespace Shooter.Game.Core.Groups
{
    [CreateAssetMenu(menuName = "Shooter/Fraction", fileName = "Fraction")]
    public class Fraction : Spec
    {
        [SerializeField] private string promptName;
        public string PromptName => promptName;
    }
}
