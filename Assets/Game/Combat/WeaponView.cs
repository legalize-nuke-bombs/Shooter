using Shooter.Game.Body;
using Shooter.Game.Core;
using Shooter.Game.Loot;
using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace Shooter.Game.Combat
{
    [RequireComponent(typeof(Inventory))]
    [RequireComponent(typeof(Skin))]
    public class WeaponView : NetworkBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private Vector3 gripAnchor = new(0.12f, -0.28f, 0.3f);
        [SerializeField] private Vector3 rightElbow = new(0.55f, 0.25f, -0.25f);
        [SerializeField] private Vector3 leftElbow = new(-0.35f, 0f, 0.15f);

        private Transform anchor;
        private Transform leftHint;
        private Transform leftShoulder;
        private Transform rightHint;
        private Transform rightShoulder;

        private Inventory inventory;
        private Skin skin;

        private WeaponRig rig;
        private Rig hold;
        private GameObject shown;
        private GameObject shownModel;

        public GameObject Shown => shown;

        private void Awake()
        {
            inventory = GetComponent<Inventory>();
            skin = GetComponent<Skin>();
        }

        private void Update()
        {
            if (shown == null || anchor == null) return;
            if (rightShoulder == null || leftShoulder == null) return;

            rightHint.localPosition = rightElbow;
            leftHint.localPosition = leftElbow;

            Vector3 chest = (rightShoulder.position + leftShoulder.position) / 2f;
            anchor.SetPositionAndRotation(chest + transform.rotation * gripAnchor, transform.rotation);
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

            Unbuild();
            if (shown != null) Destroy(shown);
            shownModel = wanted;
            shown = wanted == null ? null : Wear(wanted);

            if (rig != null) Build();
            Rebuild();
            Armed(shown != null);
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
            Animator animator = Puppet();
            if (animator == null) return null;

            GameObject worn = Instantiate(model);
            rig = worn.GetComponent<WeaponRig>();

            if (rig == null || rig.Grip == null || rig.Foregrip == null)
            {
                rig = null;
                Glue(worn, animator);
            }
            else
            {
                Raise(animator);
                Seat(worn);
            }

            Log.Info($"Entity {name} now holds {model.name}");
            return worn;
        }

        private void Glue(GameObject worn, Animator animator)
        {
            Log.Warn($"Entity {name} weapon {worn.name} has no grip pair, stays glued to the hand bone");

            Transform hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            worn.transform.SetParent(hand == null ? transform : hand, false);
            worn.transform.localPosition = Vector3.zero;
            worn.transform.localRotation = Quaternion.identity;
        }

        private void Raise(Animator animator)
        {
            rightShoulder = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            leftShoulder = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            if (rightShoulder == null || leftShoulder == null)
                Log.Warn($"Entity {name} has no shoulder bones, weapon anchor stays at the root");

            if (anchor != null) return;

            anchor = new GameObject("WeaponAnchor").transform;
            anchor.SetParent(transform, false);

            rightHint = new GameObject("RightElbowHint").transform;
            rightHint.SetParent(transform, false);

            leftHint = new GameObject("LeftElbowHint").transform;
            leftHint.SetParent(transform, false);
        }

        private void Seat(GameObject worn)
        {
            Transform root = worn.transform;
            root.SetParent(anchor, false);
            root.localPosition = rig.SeatPosition;
            root.localRotation = rig.SeatRotation;
        }

        private void Build()
        {
            Animator animator = Puppet();
            if (animator == null) return;

            if (!animator.TryGetComponent(out RigBuilder builder)) builder = animator.gameObject.AddComponent<RigBuilder>();

            var holder = new GameObject("WeaponHold");
            holder.transform.SetParent(animator.transform, false);
            hold = holder.AddComponent<Rig>();

            Arm(holder.transform, "RightArmGrip", animator, HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm,
                HumanBodyBones.RightHand, rig.Grip, rightHint);
            Arm(holder.transform, "LeftArmGrip", animator, HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm,
                HumanBodyBones.LeftHand, rig.Foregrip, leftHint);

            builder.layers.Add(new RigLayer(hold));
        }

        private static void Arm(Transform holder, string name, Animator animator, HumanBodyBones root,
            HumanBodyBones mid, HumanBodyBones tip, Transform target, Transform hint)
        {
            var constrained = new GameObject(name);
            constrained.transform.SetParent(holder, false);

            var ik = constrained.AddComponent<TwoBoneIKConstraint>();
            TwoBoneIKConstraintData data = ik.data;
            data.root = animator.GetBoneTransform(root);
            data.mid = animator.GetBoneTransform(mid);
            data.tip = animator.GetBoneTransform(tip);
            data.target = target;
            data.hint = hint;
            data.targetPositionWeight = 1f;
            data.targetRotationWeight = 1f;
            data.hintWeight = 1f;
            data.maintainTargetPositionOffset = false;
            data.maintainTargetRotationOffset = true;
            ik.data = data;
        }

        private void Unbuild()
        {
            if (hold == null) return;

            Animator animator = Puppet();
            if (animator != null && animator.TryGetComponent(out RigBuilder builder))
                builder.layers.RemoveAll(layer => layer.rig == hold);

            Destroy(hold.gameObject);
            hold = null;
            rig = null;
        }

        private void Rebuild()
        {
            Animator animator = Puppet();
            if (animator != null && animator.TryGetComponent(out RigBuilder builder)) builder.Build();
        }

        private Animator Puppet()
        {
            if (skin.Flesh == null)
            {
                Log.Warn($"Entity {name} has no flesh yet, weapon stays invisible");
                return null;
            }

            Animator animator = skin.Flesh.GetComponent<Animator>();
            if (animator == null) Log.Warn($"Entity {name} flesh has no animator, weapon stays invisible");

            return animator;
        }
    }
}
