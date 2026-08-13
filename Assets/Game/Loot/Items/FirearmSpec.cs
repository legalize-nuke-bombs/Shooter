using Shooter.Game.Body;
using Shooter.Game.Combat;
using UnityEngine;

namespace Shooter.Game.Loot
{
    [CreateAssetMenu(menuName = "Shooter/Firearm", fileName = "Firearm")]
    public class FirearmSpec : UniqueItemSpec
    {
        [SerializeField] private int magazineSize = 30;
        [SerializeField] private StackableItemSpec ammo;
        [SerializeField] private float distance = 100f;
        [SerializeField] private int damage = 25;
        [SerializeField] private float fireInterval = 0.1f;
        [SerializeField] private FireMode fireMode = FireMode.Semi;
        [SerializeField] private SprayPattern spray = new SprayPattern();
        [SerializeField] private float sprayRecovery = 0.4f;
        [SerializeField] private float recoilPunch = 0.5f;
        [SerializeField] private float reloadTime = 2.5f;
        [SerializeField] private SoundSpec shotSound;
        [SerializeField] private SoundSpec misfireSound;
        [SerializeField] private SoundSpec reloadSound;
        [SerializeField] private EarSoundSpec headshotSound;

        public int MagazineSize => Mathf.Max(magazineSize, 1);

        public StackableItemSpec Ammo => ammo;

        public float Distance => distance;

        public int Damage => damage;

        public float FireInterval => fireInterval;

        public FireMode FireMode => fireMode;

        public SprayPattern Spray => spray;

        public float SprayRecovery => sprayRecovery;

        public float RecoilPunch => recoilPunch;

        public float ReloadTime => reloadTime;

        public SoundSpec ShotSound => shotSound;

        public SoundSpec MisfireSound => misfireSound;

        public SoundSpec ReloadSound => reloadSound;

        public EarSoundSpec HeadshotSound => headshotSound;

        public override UniqueItem Create()
        {
            return new Firearm(Key);
        }
    }
}
