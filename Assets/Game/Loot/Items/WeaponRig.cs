using UnityEngine;

namespace Shooter.Game.Loot
{
    public class WeaponRig : MonoBehaviour
    {
        [SerializeField] private Vector3 seatPosition;
        [SerializeField] private Vector3 seatEuler;
        [SerializeField] private Transform grip;
        [SerializeField] private Transform foregrip;
        [SerializeField] private Transform muzzle;
        [SerializeField] private Transform bolt;
        [SerializeField] private Transform magazine;
        [SerializeField] private Transform trigger;
        [SerializeField] private Transform safety;

        public Vector3 SeatPosition => seatPosition;

        public Quaternion SeatRotation => Quaternion.Euler(seatEuler);

        public Transform Grip => grip;
        public Transform Foregrip => foregrip;
        public Transform Muzzle => muzzle;
        public Transform Bolt => bolt;
        public Transform Magazine => magazine;
        public Transform Trigger => trigger;
        public Transform Safety => safety;
    }
}
