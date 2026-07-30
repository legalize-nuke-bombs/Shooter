using UnityEngine;

namespace Shooter.Game.Body
{
    public class Skin : MonoBehaviour
    {
        [SerializeField] private SkinSpec spec;

        public SkinSpec Spec => spec;
    }
}
