using Shooter.Game.Loot;
using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Shooter.Client.Playing
{
    [RequireComponent(typeof(Inventory))]
    public class OwnWeapon : NetworkBehaviour
    {
        private const string FirstPersonLayer = "FirstPerson";

        private static readonly Journal Log = Logs.Here();

        [SerializeField] private Vector3 restPosition = new Vector3(0.325f, -0.193f, 0.654f);
        [SerializeField] private Vector3 restRotation = Vector3.zero;
        [SerializeField] private float restScale = 1f;

        private Inventory inventory;
        private Camera eye;
        private int layer = -1;

        private GameObject shownModel;
        private GameObject shown;

        public void Awake()
        {
            inventory = GetComponent<Inventory>();
            eye = GetComponentInChildren<Camera>(true);
        }

        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
            {
                enabled = false;
                return;
            }

            Overlay();
            inventory.Changed += Refresh;
            Refresh();
        }

        private void Overlay()
        {
            layer = LayerMask.NameToLayer(FirstPersonLayer);
            if (layer < 0)
            {
                Log.Warn("Own player {} found no {} layer, weapon stays in the world pass", name, FirstPersonLayer);
                return;
            }
            if (eye == null)
            {
                return;
            }

            eye.cullingMask &= ~(1 << layer);

            var volume = gameObject.AddComponent<CustomPassVolume>();
            volume.isGlobal = true;
            volume.injectionPoint = CustomPassInjectionPoint.BeforePostProcess;

            var pass = volume.AddPassOfType(typeof(FirstPersonPass)) as FirstPersonPass;
            pass.layer = 1 << layer;
            pass.eye = eye;

            Log.Info("Own player {} draws first person weapon over the world, layer {}", name, layer);
        }

        public override void OnNetworkDespawn()
        {
            inventory.Changed -= Refresh;
        }

        private void Refresh()
        {
            GameObject wanted = Wanted();
            if (wanted == shownModel) return;

            if (shown != null) Destroy(shown);
            shownModel = wanted;
            shown = wanted == null ? null : Wear(wanted);
        }

        private GameObject Wanted()
        {
            if (!inventory.Equipped(out Item item)) return null;

            return inventory.Spec(item) is FirearmSpec firearm ? firearm.Model : null;
        }

        private GameObject Wear(GameObject model)
        {
            if (eye == null)
            {
                Log.Warn("Own player {} has no camera, first person weapon stays invisible", name);
                return null;
            }

            GameObject worn = Instantiate(model, eye.transform);
            worn.transform.localPosition = restPosition;
            worn.transform.localRotation = Quaternion.Euler(restRotation);
            worn.transform.localScale = Vector3.one * restScale;

            foreach (Renderer piece in worn.GetComponentsInChildren<Renderer>(true))
                piece.shadowCastingMode = ShadowCastingMode.Off;

            if (layer >= 0)
                foreach (Transform part in worn.GetComponentsInChildren<Transform>(true))
                    part.gameObject.layer = layer;

            Log.Info("Own player {} sees {} in first person at {} scaled {} (rest {})",
                name, model.name, worn.transform.localPosition, worn.transform.localScale, restScale);
            return worn;
        }

        private void LateUpdate()
        {
            if (shown == null || eye == null) return;

            if (shown.activeSelf != eye.enabled) shown.SetActive(eye.enabled);
        }
    }
}
