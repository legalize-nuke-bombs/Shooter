using UnityEngine;

namespace Shooter.Game.Core.Groups
{
    [CreateAssetMenu(menuName = "Shooter/Group", fileName = "Group")]
    public class Group : Spec
    {
        [SerializeField] private string promptName;
        public string PromptName => promptName;
    }
}
