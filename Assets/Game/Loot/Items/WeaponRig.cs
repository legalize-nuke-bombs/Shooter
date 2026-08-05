using UnityEngine;

namespace Shooter.Game.Loot
{
    public class WeaponRig : MonoBehaviour
    {
        [SerializeField] private Transform muzzle;
        [SerializeField] private Transform bolt;
        [SerializeField] private Transform magazine;
        [SerializeField] private Transform trigger;
        [SerializeField] private Transform safety;

        public Transform Muzzle => muzzle;
        public Transform Bolt => bolt;
        public Transform Magazine => magazine;
        public Transform Trigger => trigger;
        public Transform Safety => safety;
    }
}
