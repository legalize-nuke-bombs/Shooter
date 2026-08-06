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

        private WeaponRig rig;
        private Transform rightHand;
        private Transform leftHand;

        private void Awake()
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
            Log.Info("Entity {} refresh: wanted {}, shown {}", name,
                wanted == null ? "nothing" : wanted.name,
                shownModel == null ? "nothing" : shownModel.name);
            if (wanted == shownModel) return;

            if (shown != null) Destroy(shown);
            shownModel = wanted;
            shown = wanted == null ? null : Wear(wanted);

            Armed(wanted != null);
        }

        private void Armed(bool armed)
        {
            if (skin.Flesh == null) return;

            var animator = skin.Flesh.GetComponent<Animator>();
            if (animator == null) return;

            int layer = animator.GetLayerIndex("Armed");
            Log.Info("Entity {} armed {}, layer index {}", name, armed, layer);
            if (layer >= 0) animator.SetLayerWeight(layer, armed ? 1f : 0f);
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
            Anchor(worn);

            if (IsLocalPlayer) Conceal(worn);

            Log.Info("Entity {} now holds {}", name, model.name);
            return worn;
        }

        private Transform Hand()
        {
            if (skin.Flesh == null)
            {
                Log.Warn("Entity {} has no flesh yet, weapon stays invisible", name);
                return null;
            }

            var animator = skin.Flesh.GetComponent<Animator>();
            if (animator == null)
            {
                Log.Warn("Entity {} flesh has no animator, weapon stays invisible", name);
                return null;
            }

            Transform hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            if (hand == null) Log.Warn("Entity {} has no right hand bone, weapon stays invisible", name);

            return hand;
        }

        private void Anchor(GameObject worn)
        {
            worn.transform.localPosition = Vector3.zero;
            worn.transform.localRotation = Quaternion.identity;

            rig = worn.GetComponent<WeaponRig>();
            if (rig == null || rig.Grip == null || rig.Foregrip == null)
            {
                Log.Warn("Entity {} weapon {} has no grip pair, stays glued to the hand bone", name, worn.name);
                rig = null;
                return;
            }

            var animator = skin.Flesh.GetComponent<Animator>();
            rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
        }

        private void LateUpdate()
        {
            if (shown == null || rig == null || rig.Grip == null || rig.Foregrip == null) return;
            if (rightHand == null || leftHand == null) return;

            Vector3 aim = leftHand.position - rightHand.position;
            if (aim.sqrMagnitude < 0.0001f) return;

            Transform root = shown.transform;
            Vector3 gripPoint = root.InverseTransformPoint(rig.Grip.position);
            Vector3 barrelAxis = root.InverseTransformDirection((rig.Foregrip.position - rig.Grip.position).normalized);
            Vector3 barrelUp = root.InverseTransformDirection(rig.Grip.up);

            Quaternion look = Quaternion.LookRotation(aim, Vector3.up);
            Quaternion barrel = Quaternion.LookRotation(barrelAxis, barrelUp);

            shown.transform.rotation = look * Quaternion.Inverse(barrel);
            shown.transform.position = rightHand.position - shown.transform.rotation * gripPoint;
        }

        private void Conceal(GameObject worn)
        {
            foreach (Renderer renderer in worn.GetComponentsInChildren<Renderer>())
                renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
        }
    }
}
