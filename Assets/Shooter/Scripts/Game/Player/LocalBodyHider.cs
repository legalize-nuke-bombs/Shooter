using System.Collections.Generic;
using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

namespace Shooter.Game
{
    [RequireComponent(typeof(Player))]
    [RequireComponent(typeof(Skin))]
    [RequireComponent(typeof(WeaponView))]
    public class LocalBodyHider : ShooterNetworkBehaviour
    {
        private static readonly Journal Log = Logs.Here();

        private Player player;
        private Skin skin;
        private WeaponView weaponView;
        private GameObject hiddenModelInstance;
        private bool hiding;
        private bool needsHiding;

        protected override void ShooterAwake()
        {
            player = GetComponent<Player>();
            skin = GetComponent<Skin>();
            weaponView = GetComponent<WeaponView>();
            enabled = false;
        }

        private void LateUpdate()
        {
            if (needsHiding || hiddenModelInstance != skin.ModelInstance) HideModelAndHeldWeapon();
        }

        public override void OnNetworkSpawn()
        {
            player.ControlChanged += HandleControlChanged;
            HandleControlChanged();
        }

        public override void OnNetworkDespawn()
        {
            player.ControlChanged -= HandleControlChanged;
            StopHidingBody();
        }

        private void HandleControlChanged()
        {
            if (player.IsControlledLocally)
            {
                StartHidingBody();
            }
            else
            {
                StopHidingBody();
            }
        }

        private void StartHidingBody()
        {
            if (hiding) return;
            hiding = true;
            needsHiding = true;
            enabled = true;

            weaponView.ShownWeaponChanged += MarkHidingNeeded;

            Log.Info($"Local player {name} hides its body from its own camera");
        }

        private void StopHidingBody()
        {
            if (!hiding) return;
            hiding = false;
            enabled = false;

            weaponView.ShownWeaponChanged -= MarkHidingNeeded;

            SetShadowCastingMode(ShadowCastingMode.On);
            hiddenModelInstance = null;
        }

        private void MarkHidingNeeded()
        {
            needsHiding = true;
        }

        private void HideModelAndHeldWeapon()
        {
            needsHiding = false;
            hiddenModelInstance = skin.ModelInstance;

            if (hiddenModelInstance == null)
            {
                Log.Warn($"Local player {name} has no model to hide");
                return;
            }

            SetShadowCastingMode(ShadowCastingMode.ShadowsOnly);
        }

        private void SetShadowCastingMode(ShadowCastingMode mode)
        {
            foreach (Renderer piece in GetModelAndHeldWeaponRenderers(skin.ModelInstance))
            {
                piece.shadowCastingMode = mode;
            }
        }

        private IEnumerable<Renderer> GetModelAndHeldWeaponRenderers(GameObject model)
        {
            if (model != null)
            {
                foreach (Renderer piece in model.GetComponentsInChildren<Renderer>(true))
                {
                    yield return piece;
                }
            }

            GameObject heldWeapon = weaponView.ShownWeapon;
            if (heldWeapon == null) yield break;

            foreach (Renderer piece in heldWeapon.GetComponentsInChildren<Renderer>(true))
            {
                yield return piece;
            }
        }
    }
}
