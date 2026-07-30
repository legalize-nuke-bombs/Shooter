using UnityEngine;

namespace Shooter.Game.Body
{
    [CreateAssetMenu(menuName = "Shooter/Skin", fileName = "Skin")]
    public class SkinSpec : Spec
    {
        [SerializeField] private GameObject model;

        public GameObject Model => model;
    }
}
