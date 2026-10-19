using Shooter.Infrastructure;
using UnityEngine;

namespace Shooter.Game
{
    [CreateAssetMenu(menuName = "Shooter/Skin", fileName = "Skin")]
    public class SkinAsset : Asset
    {
        [SerializeField] private GameObject model;
        [SerializeField] private RuntimeAnimatorController pose;

        public GameObject Model => model;

        public RuntimeAnimatorController AnimatorController => pose;
    }
}
