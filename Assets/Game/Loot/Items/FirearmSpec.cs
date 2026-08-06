using Shooter.Game.Body.Sounding;
using UnityEngine;

namespace Shooter.Game.Loot
{
    [CreateAssetMenu(menuName = "Shooter/Firearm", fileName = "Firearm")]
    public class FirearmSpec : ItemSpec
    {
        [SerializeField] private GameObject model;
        [SerializeField] private int magazineSize = 30;
        [SerializeField] private ItemSpec ammo;
        [SerializeField] private float distance = 100f;
        [SerializeField] private int damage = 25;
        [SerializeField] private float fireInterval = 0.1f;
        [SerializeField] private float reloadTime = 2.5f;
        [SerializeField] private SoundSpec shotSound;
        [SerializeField] private SoundSpec misfireSound;
        [SerializeField] private SoundSpec reloadSound;
        [SerializeField] private SoundSpec headshotSound;

        public GameObject Model => model;

        public int MagazineSize => Mathf.Max(magazineSize, 1);

        public ItemSpec Ammo => ammo;

        public float Distance => distance;

        public int Damage => damage;

        public float FireInterval => fireInterval;

        public float ReloadTime => reloadTime;

        public SoundSpec ShotSound => shotSound;

        public SoundSpec MisfireSound => misfireSound;

        public SoundSpec ReloadSound => reloadSound;

        public SoundSpec HeadshotSound => headshotSound;
    }
}
