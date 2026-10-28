using UnityEngine;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    public class Hitbox : MonoBehaviour
    {
        public BodyPart Part { get; set; }
    }
}
