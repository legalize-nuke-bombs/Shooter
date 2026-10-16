using UnityEngine;
using Shooter.Infrastructure;

namespace Shooter.Game
{
    [CreateAssetMenu(menuName = "Shooter/Fraction", fileName = "Fraction")]
    public class Fraction : Spec
    {
        [SerializeField] private string promptName;
        public string PromptName => promptName;
    }
}
