using Shooter.Game.Loot;
using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

namespace Shooter.Client.Playing
{
    [RequireComponent(typeof(Inventory))]
    public class OwnWeapon : NetworkBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private Vector3 restPosition = new Vector3(0.23f, -0.24f, 0.38f);
        [SerializeField] private Vector3 restRotation = Vector3.zero;

        private Inventory inventory;
        private Camera eye;

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

            inventory.Changed += Refresh;
            Refresh();
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

            foreach (Renderer piece in worn.GetComponentsInChildren<Renderer>(true))
                piece.shadowCastingMode = ShadowCastingMode.Off;

            Log.Info("Own player {} sees {} in first person", name, model.name);
            return worn;
        }

        private void LateUpdate()
        {
            if (shown == null || eye == null) return;

            if (shown.activeSelf != eye.enabled) shown.SetActive(eye.enabled);
        }
    }
}
