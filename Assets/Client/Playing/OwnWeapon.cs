using Shooter.Game.Core;
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

        [SerializeField] private Vector3 restPosition = new(0.325f, -0.193f, 0.654f);
        [SerializeField] private Vector3 restRotation = Vector3.zero;
        [SerializeField] private float restScale = 1f;
        private Camera eye;

        private Inventory inventory;
        private int layer = -1;
        private GameObject shown;

        private GameObject shownModel;
        private CustomPassVolume volume;
        private bool active;

        private void Awake()
        {
            inventory = GetComponent<Inventory>();
            eye = GetComponentInChildren<Camera>();
        }

        private void LateUpdate()
        {
            if (shown == null || eye == null) return;

            if (shown.activeSelf != eye.enabled) shown.SetActive(eye.enabled);
        }

        public override void OnNetworkSpawn()
        {
            if (IsOwner) Activate();
            else enabled = false;
        }

        public override void OnGainedOwnership()
        {
            enabled = true;
            Activate();
        }

        public override void OnLostOwnership()
        {
            Deactivate();
            enabled = false;
        }

        public override void OnNetworkDespawn()
        {
            Deactivate();
        }

        private void Activate()
        {
            if (active) return;
            active = true;

            Overlay();
            inventory.Changed += Refresh;
            Refresh();
        }

        private void Deactivate()
        {
            if (!active) return;
            active = false;

            inventory.Changed -= Refresh;
            if (shown != null) Destroy(shown);
            shown = null;
            shownModel = null;
        }

        private void Overlay()
        {
            if (volume != null) return;

            layer = LayerMask.NameToLayer(FirstPersonLayer);
            if (layer < 0)
            {
                Log.Warn(
                    $"Own player {name} found no {FirstPersonLayer} layer, weapon stays in the world pass");
                return;
            }

            if (eye == null) return;

            eye.cullingMask &= ~(1 << layer);

            volume = gameObject.AddComponent<CustomPassVolume>();
            volume.targetCamera = eye;
            volume.injectionPoint = CustomPassInjectionPoint.BeforePostProcess;

            var pass = volume.AddPassOfType(typeof(FirstPersonPass)) as FirstPersonPass;
            pass.layer = 1 << layer;

            Log.Info($"Own player {name} draws first person weapon over the world, layer {layer}");
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
            return inventory.EquippedSpec is FirearmSpec firearm ? firearm.Model : null;
        }

        private GameObject Wear(GameObject model)
        {
            if (eye == null)
            {
                Log.Warn($"Own player {name} has no camera, first person weapon stays invisible");
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

            Log.Info(
                $"Own player {name} sees {model.name} in first person at {worn.transform.localPosition} scaled {worn.transform.localScale} (rest {restScale})");
            return worn;
        }
    }
}
