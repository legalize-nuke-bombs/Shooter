using Shooter.Game.Body;
using Shooter.Game.Loot;
using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

namespace Shooter.Game.Combat
{
    [RequireComponent(typeof(Inventory))]
    [RequireComponent(typeof(Skin))]
    public class WeaponView : NetworkBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        private Inventory inventory;
        private Skin skin;

        private GameObject shownModel;
        private GameObject shown;

        public void Awake()
        {
            inventory = GetComponent<Inventory>();
            skin = GetComponent<Skin>();
        }

        public override void OnNetworkSpawn()
        {
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
            Transform hand = Hand();
            if (hand == null) return null;

            GameObject worn = Instantiate(model, hand);
            Held(worn);

            if (IsLocalPlayer) Conceal(worn);

            Log.Info("Entity {} now holds {}", name, model.name);
            return worn;
        }

        private Transform Hand()
        {
            if (skin.Flesh == null) return null;

            var animator = skin.Flesh.GetComponent<Animator>();
            if (animator == null) return null;

            Transform hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            if (hand == null) Log.Warn("Entity {} has no right hand bone, weapon stays invisible", name);

            return hand;
        }

        private void Held(GameObject worn)
        {
            WeaponRig rig = worn.GetComponent<WeaponRig>();
            if (rig == null || rig.Grip == null)
            {
                worn.transform.localPosition = Vector3.zero;
                worn.transform.localRotation = Quaternion.identity;
                return;
            }

            Quaternion gripRotation = Quaternion.Inverse(worn.transform.rotation) * rig.Grip.rotation;
            Vector3 gripPosition = worn.transform.InverseTransformPoint(rig.Grip.position);

            worn.transform.localRotation = Quaternion.Inverse(gripRotation);
            worn.transform.localPosition = -(worn.transform.localRotation * gripPosition);
        }

        private void Conceal(GameObject worn)
        {
            foreach (Renderer renderer in worn.GetComponentsInChildren<Renderer>())
                renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
        }
    }
}
