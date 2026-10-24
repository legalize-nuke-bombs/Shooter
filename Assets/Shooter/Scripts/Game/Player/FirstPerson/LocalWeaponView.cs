using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Shooter.Game
{
    [RequireComponent(typeof(Player))]
    [RequireComponent(typeof(InventoryHolder))]
    public class LocalWeaponView : ShooterNetworkBehaviour
    {
        private const string FirstPersonLayerName = "FirstPerson";

        private static readonly Journal Log = Logs.Here();

        [SerializeField] private Vector3 restPosition = new(0.325f, -0.193f, 0.654f);
        [SerializeField] private Vector3 restRotation = Vector3.zero;
        [SerializeField] private float restScale = 1f;

        private Player player;
        private Camera view;
        private Inventory inventory;
        private InventoryHolder inventoryHolder;
        private int firstPersonLayer = -1;
        private GameObject shownWeapon;
        private GameObject shownWeaponModel;
        private CustomPassVolume customPassVolume;
        private Transform weaponHold;
        private bool showing;

        public Transform WeaponHold => shownWeapon == null ? null : weaponHold;

        protected override void ShooterAwake()
        {
            player = GetComponent<Player>();
            inventory = GetComponent<Inventory>();
            inventoryHolder = GetComponent<InventoryHolder>();
            view = GetComponentInChildren<Camera>();
        }

        private void LateUpdate()
        {
            if (shownWeapon == null || view == null) return;

            if (shownWeapon.activeSelf != view.enabled) shownWeapon.SetActive(view.enabled);
        }

        public override void OnNetworkSpawn()
        {
            player.ControlChanged += HandleControlChanged;
            HandleControlChanged();
        }

        public override void OnNetworkDespawn()
        {
            player.ControlChanged -= HandleControlChanged;
            StopShowingHeldWeapon();
        }

        private void HandleControlChanged()
        {
            if (player.IsControlledLocally)
            {
                enabled = true;
                StartShowingHeldWeapon();
            }
            else
            {
                StopShowingHeldWeapon();
                enabled = false;
            }
        }

        private void StartShowingHeldWeapon()
        {
            if (showing) return;
            showing = true;

            AddFirstPersonPass();
            inventory.Changed += UpdateShownWeapon;
            inventoryHolder.Changed += UpdateShownWeapon;
            UpdateShownWeapon();
        }

        private void StopShowingHeldWeapon()
        {
            if (!showing) return;
            showing = false;

            inventory.Changed -= UpdateShownWeapon;
            inventoryHolder.Changed -= UpdateShownWeapon;
            if (shownWeapon != null) Destroy(shownWeapon);
            if (weaponHold != null) Destroy(weaponHold.gameObject);
            shownWeapon = null;
            shownWeaponModel = null;
            weaponHold = null;
        }

        private void AddFirstPersonPass()
        {
            if (customPassVolume != null) return;

            firstPersonLayer = LayerMask.NameToLayer(FirstPersonLayerName);
            if (firstPersonLayer < 0)
            {
                Log.Error($"Local player {name} found no {FirstPersonLayerName} layer, weapon stays in the world pass");
                return;
            }

            if (view == null) return;

            view.cullingMask &= ~(1 << firstPersonLayer);

            customPassVolume = gameObject.AddComponent<CustomPassVolume>();
            customPassVolume.targetCamera = view;
            customPassVolume.injectionPoint = CustomPassInjectionPoint.BeforePostProcess;

            FirstPersonPass firstPersonPass = customPassVolume.AddPassOfType(typeof(FirstPersonPass)) as FirstPersonPass;
            firstPersonPass.layer = 1 << firstPersonLayer;

            Log.Info($"Local player {name} draws first person weapon over the world, layer {firstPersonLayer}");
        }

        private void UpdateShownWeapon()
        {
            GameObject heldModel = GetHeldFirearmModel();
            if (heldModel == shownWeaponModel) return;

            if (shownWeapon != null) Destroy(shownWeapon);
            shownWeaponModel = heldModel;
            shownWeapon = heldModel == null ? null : InstantiateFirstPersonWeapon(heldModel);
        }

        private GameObject GetHeldFirearmModel()
        {
            return inventoryHolder.HeldAsset is FirearmAsset firearm ? firearm.Model : null;
        }

        private GameObject InstantiateFirstPersonWeapon(GameObject model)
        {
            if (view == null)
            {
                Log.Error($"Local player {name} has no camera, first person weapon stays invisible");
                return null;
            }

            if (weaponHold == null)
            {
                weaponHold = new GameObject("FirstPersonHold").transform;
                weaponHold.SetParent(view.transform, false);
            }

            GameObject weapon = Instantiate(model, weaponHold);
            weapon.transform.localPosition = restPosition;
            weapon.transform.localRotation = Quaternion.Euler(restRotation);
            weapon.transform.localScale = Vector3.one * restScale;

            foreach (Renderer piece in weapon.GetComponentsInChildren<Renderer>(true))
            {
                piece.shadowCastingMode = ShadowCastingMode.Off;
            }

            if (firstPersonLayer >= 0)
            {
                foreach (Transform part in weapon.GetComponentsInChildren<Transform>(true))
                {
                    part.gameObject.layer = firstPersonLayer;
                }
            }

            Log.Info($"Local player {name} sees {model.name} in first person at {weapon.transform.localPosition} scaled {weapon.transform.localScale} (rest {restScale})");
            return weapon;
        }
    }
}
