using UnityEngine;

namespace Shooter.Game.Core
{
    [CreateAssetMenu(menuName = "Shooter/Icon", fileName = "Icon")]
    public class IconSpec : Spec
    {
        [SerializeField] private Sprite sprite;
        [SerializeField] private string promptDescription;

        public Sprite Sprite => sprite;
        public string PromptDescription => promptDescription;
    }
}
