using UnityEngine;
using Shooter.Game.Body.Sounding;

namespace Shooter.Game.Loot
{
    [CreateAssetMenu(menuName = "Shooter/Firearm", fileName = "Firearm")]
    public class FirearmSpec : ItemSpec
    {
        [SerializeField] private int magazineSize = 30;
        [SerializeField] private ItemType ammoType = ItemType.Ammo762X39;
        [SerializeField] private float distance = 100f;
        [SerializeField] private int damage = 25;
        [SerializeField] private float fireInterval = 0.1f;
        [SerializeField] private float reloadTime = 2.5f;
        [SerializeField] private SoundType shotSound = SoundType.Ak47Shot;
        [SerializeField] private SoundType misfireSound = SoundType.Ak47Misfire;
        [SerializeField] private SoundType reloadSound = SoundType.Ak47Reload;

        public int MagazineSize => Mathf.Max(magazineSize, 1);

        public ItemType AmmoType => ammoType;

        public float Distance => distance;

        public int Damage => damage;

        public float FireInterval => fireInterval;

        public float ReloadTime => reloadTime;

        public SoundType ShotSound => shotSound;

        public SoundType MisfireSound => misfireSound;

        public SoundType ReloadSound => reloadSound;
    }
}
