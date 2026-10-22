using UnityEngine;

namespace Shooter.Game
{
    [CreateAssetMenu(menuName = "Shooter/Firearm", fileName = "Firearm")]
    public class FirearmAsset : UniqueItemAsset
    {
        [SerializeField] private int magazineSize = 30;
        [SerializeField] private StackableItemAsset ammo;
        [SerializeField] private float distance = 100f;
        [SerializeField] private int damage = 25;
        [SerializeField] private DamageAsset damageType;
        [SerializeField] private float fireInterval = 0.1f;
        [SerializeField] private FireMode fireMode = FireMode.Semi;
        [SerializeField] private SprayPattern spray = new();
        [SerializeField] private float sprayRecovery = 0.4f;
        [SerializeField] private float recoilPunch = 0.5f;
        [SerializeField] private float reloadTime = 2.5f;
        [SerializeField] private SoundAsset shotSound;
        [SerializeField] private SoundAsset misfireSound;
        [SerializeField] private SoundAsset reloadSound;
        [SerializeField] private EarSoundAsset headshotSound;
        [SerializeField] private SurfaceSounds impactSounds;

        public int MagazineSize => Mathf.Max(magazineSize, 1);

        public StackableItemAsset Ammo => ammo;

        public float Distance => distance;

        public int Damage => damage;

        public DamageAsset DamageType => damageType;

        public float FireInterval => fireInterval;

        public FireMode FireMode => fireMode;

        public SprayPattern Spray => spray;

        public float SprayRecovery => sprayRecovery;

        public float RecoilPunch => recoilPunch;

        public float ReloadTime => reloadTime;

        public SoundAsset ShotSound => shotSound;

        public SoundAsset MisfireSound => misfireSound;

        public SoundAsset ReloadSound => reloadSound;

        public EarSoundAsset HeadshotSound => headshotSound;

        public SurfaceSounds ImpactSounds => impactSounds;

        public override UniqueItem CreateItem()
        {
            return new Firearm(Id);
        }
    }
}
