using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;

namespace Shooter.Game
{
    [RequireComponent(typeof(InventoryHolder))]
    [RequireComponent(typeof(Skin))]
    public class WeaponView : NetworkBehaviour
    {
        private static readonly Journal Log = Logs.CreateJournalForThisFile();

        [SerializeField] private Vector3 gripAnchor = new(0.12f, -0.28f, 0.3f);
        [SerializeField] private Vector3 rightElbow = new(0.55f, 0.25f, -0.25f);
        [SerializeField] private Vector3 leftElbow = new(-0.35f, 0f, 0.15f);

        private Transform weaponAnchor;
        private Transform leftElbowHint;
        private Transform leftShoulder;
        private Transform rightElbowHint;
        private Transform rightShoulder;

        private InventoryHolder inventoryHolder;
        private Skin skin;

        private WeaponRig weaponRig;
        private Rig weaponHoldRig;
        private GameObject shownWeapon;
        private GameObject shownWeaponModel;

        public GameObject ShownWeapon => shownWeapon;

        private void Awake()
        {
            inventoryHolder = GetComponent<InventoryHolder>();
            skin = GetComponent<Skin>();
        }

        private void Update()
        {
            if (shownWeapon == null || weaponAnchor == null) return;
            if (rightShoulder == null || leftShoulder == null) return;

            rightElbowHint.localPosition = rightElbow;
            leftElbowHint.localPosition = leftElbow;

            Vector3 chest = (rightShoulder.position + leftShoulder.position) / 2f;
            weaponAnchor.SetPositionAndRotation(chest + transform.rotation * gripAnchor, transform.rotation);
        }

        public override void OnNetworkSpawn()
        {
            inventoryHolder.Changed += UpdateShownWeapon;
            UpdateShownWeapon();
        }

        public override void OnNetworkDespawn()
        {
            inventoryHolder.Changed -= UpdateShownWeapon;
        }

        private void UpdateShownWeapon()
        {
            GameObject heldWeaponModel = GetHeldWeaponModel();
            Log.Debug($"Entity {name} refresh: wanted {(heldWeaponModel == null ? "nothing" : heldWeaponModel.name)}, shown {(shownWeaponModel == null ? "nothing" : shownWeaponModel.name)}");
            if (heldWeaponModel == shownWeaponModel) return;

            DestroyWeaponHoldRig();
            if (shownWeapon != null) Destroy(shownWeapon);
            shownWeaponModel = heldWeaponModel;
            shownWeapon = heldWeaponModel == null ? null : InstantiateWeaponInHands(heldWeaponModel);

            if (weaponRig != null) BuildWeaponHoldRig();
            RebuildRigBuilder();
            SetArmedLayerWeight(shownWeapon != null);
        }

        private void SetArmedLayerWeight(bool isArmed)
        {
            if (skin.ModelInstance == null) return;

            Animator animator = skin.ModelInstance.GetComponent<Animator>();
            if (animator == null) return;

            int layer = animator.GetLayerIndex("Armed");
            Log.Info($"Entity {name} armed {isArmed}, layer index {layer}");
            if (layer >= 0) animator.SetLayerWeight(layer, isArmed ? 1f : 0f);
        }

        private GameObject GetHeldWeaponModel()
        {
            return inventoryHolder.HeldAsset is FirearmAsset firearm ? firearm.Model : null;
        }

        private GameObject InstantiateWeaponInHands(GameObject weaponModel)
        {
            Animator animator = GetSkinAnimator();
            if (animator == null) return null;

            GameObject wornWeapon = Instantiate(weaponModel);
            weaponRig = wornWeapon.GetComponent<WeaponRig>();

            if (weaponRig == null || weaponRig.Grip == null || weaponRig.Foregrip == null)
            {
                weaponRig = null;
                GlueToHandBone(wornWeapon, animator);
            }
            else
            {
                FindShouldersAndCreateWeaponAnchor(animator);
                SeatOnWeaponAnchor(wornWeapon);
            }

            Log.Info($"Entity {name} now holds {weaponModel.name}");
            return wornWeapon;
        }

        private void GlueToHandBone(GameObject wornWeapon, Animator animator)
        {
            Log.Warn($"Entity {name} weapon {wornWeapon.name} has no grip pair, stays glued to the hand bone");

            Transform hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            wornWeapon.transform.SetParent(hand == null ? transform : hand, false);
            wornWeapon.transform.localPosition = Vector3.zero;
            wornWeapon.transform.localRotation = Quaternion.identity;
        }

        private void FindShouldersAndCreateWeaponAnchor(Animator animator)
        {
            rightShoulder = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            leftShoulder = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            if (rightShoulder == null || leftShoulder == null)
            {
                Log.Warn($"Entity {name} has no shoulder bones, weapon anchor stays at the root");
            }

            if (weaponAnchor != null) return;

            weaponAnchor = new GameObject("WeaponAnchor").transform;
            weaponAnchor.SetParent(transform, false);

            rightElbowHint = new GameObject("RightElbowHint").transform;
            rightElbowHint.SetParent(transform, false);

            leftElbowHint = new GameObject("LeftElbowHint").transform;
            leftElbowHint.SetParent(transform, false);
        }

        private void SeatOnWeaponAnchor(GameObject wornWeapon)
        {
            Transform weaponTransform = wornWeapon.transform;
            weaponTransform.SetParent(weaponAnchor, false);
            weaponTransform.localPosition = weaponRig.SeatPosition;
            weaponTransform.localRotation = weaponRig.SeatRotation;
        }

        private void BuildWeaponHoldRig()
        {
            Animator animator = GetSkinAnimator();
            if (animator == null) return;

            if (!animator.TryGetComponent(out RigBuilder rigBuilder)) rigBuilder = animator.gameObject.AddComponent<RigBuilder>();

            GameObject weaponHoldObject = new("WeaponHold");
            weaponHoldObject.transform.SetParent(animator.transform, false);
            weaponHoldRig = weaponHoldObject.AddComponent<Rig>();

            AddArmIkConstraint(weaponHoldObject.transform, "RightArmGrip", animator, HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, weaponRig.Grip, rightElbowHint);
            AddArmIkConstraint(weaponHoldObject.transform, "LeftArmGrip", animator, HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, weaponRig.Foregrip, leftElbowHint);

            rigBuilder.layers.Add(new RigLayer(weaponHoldRig));
        }

        private static void AddArmIkConstraint(Transform parent, string constraintName, Animator animator, HumanBodyBones root, HumanBodyBones mid, HumanBodyBones tip, Transform target, Transform hint)
        {
            GameObject constraintObject = new(constraintName);
            constraintObject.transform.SetParent(parent, false);

            TwoBoneIKConstraint ikConstraint = constraintObject.AddComponent<TwoBoneIKConstraint>();
            TwoBoneIKConstraintData data = ikConstraint.data;
            data.root = animator.GetBoneTransform(root);
            data.mid = animator.GetBoneTransform(mid);
            data.tip = animator.GetBoneTransform(tip);
            data.target = target;
            data.hint = hint;
            data.targetPositionWeight = 1f;
            data.targetRotationWeight = 1f;
            data.hintWeight = 1f;
            data.maintainTargetPositionOffset = false;
            data.maintainTargetRotationOffset = false;
            ikConstraint.data = data;
        }

        private void DestroyWeaponHoldRig()
        {
            if (weaponHoldRig == null) return;

            Animator animator = GetSkinAnimator();
            if (animator != null && animator.TryGetComponent(out RigBuilder rigBuilder))
            {
                rigBuilder.layers.RemoveAll(layer => layer.rig == weaponHoldRig);
            }

            // Destroy waits for the end of the frame, and until then a new WeaponHold would bind to this one by name
            weaponHoldRig.transform.SetParent(null, false);
            Destroy(weaponHoldRig.gameObject);
            weaponHoldRig = null;
            weaponRig = null;
        }

        private void RebuildRigBuilder()
        {
            Animator animator = GetSkinAnimator();
            if (animator == null || !animator.TryGetComponent(out RigBuilder rigBuilder)) return;

            // Animation Rigging never unbinds its handles: left alone, they outlive a removed WeaponHold and warn on every rebind
            animator.UnbindAllStreamHandles();
            animator.UnbindAllSceneHandles();
            rigBuilder.Build();
        }

        private Animator GetSkinAnimator()
        {
            if (skin.ModelInstance == null)
            {
                Log.Warn($"Entity {name} has no model yet, weapon stays invisible");
                return null;
            }

            Animator animator = skin.ModelInstance.GetComponent<Animator>();
            if (animator == null) Log.Warn($"Entity {name} model has no animator, weapon stays invisible");

            return animator;
        }
    }
}
