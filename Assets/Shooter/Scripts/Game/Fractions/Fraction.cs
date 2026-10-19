using UnityEngine;
using Shooter.Infrastructure;

namespace Shooter.Game
{
    [CreateAssetMenu(menuName = "Shooter/Fraction", fileName = "Fraction")]
    public class Fraction : Asset
    {
        [SerializeField] private string promptName;
        public string PromptName => promptName;
    }
}
