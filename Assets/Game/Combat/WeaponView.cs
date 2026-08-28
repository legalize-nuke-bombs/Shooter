using Shooter.Game.Body;
using Shooter.Game.Core;
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
        private Transform leftHand;

        private WeaponRig rig;
        private Transform rightHand;
        private GameObject shown;

        private GameObject shownModel;
        private Skin skin;

        private void Awake()
        {
            inventory = GetComponent<Inventory>();
            skin = GetComponent<Skin>();
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

            var look = Quaternion.LookRotation(aim, Vector3.up);
            var barrel = Quaternion.LookRotation(barrelAxis, barrelUp);

            shown.transform.rotation = look * Quaternion.Inverse(barrel);
            shown.transform.position = rightHand.position - shown.transform.rotation * gripPoint;
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
            Log.Info(
                $"Entity {name} refresh: wanted {(wanted == null ? "nothing" : wanted.name)}, shown {(shownModel == null ? "nothing" : shownModel.name)}");
            if (wanted == shownModel) return;

            if (shown != null) Destroy(shown);
            shownModel = wanted;
            shown = wanted == null ? null : Wear(wanted);

            Armed(wanted != null);
        }

        private void Armed(bool armed)
        {
            if (skin.Flesh == null) return;

            Animator animator = skin.Flesh.GetComponent<Animator>();
            if (animator == null) return;

            int layer = animator.GetLayerIndex("Armed");
            Log.Info($"Entity {name} armed {armed}, layer index {layer}");
            if (layer >= 0) animator.SetLayerWeight(layer, armed ? 1f : 0f);
        }

        private GameObject Wanted()
        {
            return inventory.EquippedSpec is FirearmSpec firearm ? firearm.Model : null;
        }

        private GameObject Wear(GameObject model)
        {
            Transform hand = Hand();
            if (hand == null) return null;

            GameObject worn = Instantiate(model, hand);
            Anchor(worn);
            MatchBodyShadows(worn);

            Log.Info($"Entity {name} now holds {model.name}");
            return worn;
        }

        private Transform Hand()
        {
            if (skin.Flesh == null)
            {
                Log.Warn($"Entity {name} has no flesh yet, weapon stays invisible");
                return null;
            }

            Animator animator = skin.Flesh.GetComponent<Animator>();
            if (animator == null)
            {
                Log.Warn($"Entity {name} flesh has no animator, weapon stays invisible");
                return null;
            }

            Transform hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            if (hand == null) Log.Warn($"Entity {name} has no right hand bone, weapon stays invisible");

            return hand;
        }

        private void Anchor(GameObject worn)
        {
            worn.transform.localPosition = Vector3.zero;
            worn.transform.localRotation = Quaternion.identity;

            rig = worn.GetComponent<WeaponRig>();
            if (rig == null || rig.Grip == null || rig.Foregrip == null)
            {
                Log.Warn($"Entity {name} weapon {worn.name} has no grip pair, stays glued to the hand bone");
                rig = null;
                return;
            }

            Animator animator = skin.Flesh.GetComponent<Animator>();
            rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
        }

        private void MatchBodyShadows(GameObject worn)
        {
            SkinnedMeshRenderer body = skin.Flesh.GetComponentInChildren<SkinnedMeshRenderer>();
            if (body == null) return;

            foreach (Renderer renderer in worn.GetComponentsInChildren<Renderer>(true))
                renderer.shadowCastingMode = body.shadowCastingMode;
        }
    }
}
