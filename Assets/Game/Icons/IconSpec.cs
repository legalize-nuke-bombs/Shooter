using UnityEngine;

namespace Shooter.Game.Icons
{
    [CreateAssetMenu(menuName = "Shooter/Icon", fileName = "Icon")]
    public class IconSpec : Spec
    {
        [SerializeField] private Sprite sprite;

        public Sprite Sprite => sprite;
    }
}
