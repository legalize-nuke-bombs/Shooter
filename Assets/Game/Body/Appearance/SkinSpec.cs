using UnityEngine;

namespace Shooter.Game.Body
{
    [CreateAssetMenu(menuName = "Shooter/Skin", fileName = "Skin")]
    public class SkinSpec : Spec
    {
        [SerializeField] private GameObject model;
        [SerializeField] private RuntimeAnimatorController pose;

        public GameObject Model => model;

        public RuntimeAnimatorController Pose => pose;
    }
}
