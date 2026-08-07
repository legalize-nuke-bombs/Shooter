using Shooter.Client.Playing;
using Shooter.Game.Loot;
using Shooter.Logging;
using UnityEngine.UIElements;

namespace Shooter.Client.Interface.Overlays
{
    public class AmmoOverlay : Overlay
    {
        private static readonly Journal Log = Logs.Here();

        private const string BoxElement = "ammo";
        private const string MagazineElement = "ammo-magazine";
        private const string ReserveElement = "ammo-reserve";
        private const int Hidden = -1;

        private VisualElement box;
        private Label magazine;
        private Label reserve;
        private Inventory bag;
        private int shownMagazine = Hidden;
        private int shownReserve = Hidden;

        private void Update()
        {
            if (!Bound) return;

            Inventory own = Own();
            var firearm = own == null ? null : own.Equipped() as Firearm;
            var spec = own == null ? null : own.Spec(firearm) as FirearmSpec;

            if (firearm == null || spec == null)
            {
                Hide();
                return;
            }

            int held = firearm.Magazine;
            int left = spec.Ammo == null ? 0 : own.Amount(spec.Ammo.Id);

            if (held == shownMagazine && left == shownReserve) return;

            shownMagazine = held;
            shownReserve = left;

            box.style.display = DisplayStyle.Flex;
            magazine.text = held.ToString();
            reserve.text = left.ToString();
        }

        protected override bool Bind(VisualElement root)
        {
            box = root.Q<VisualElement>(BoxElement);
            magazine = root.Q<Label>(MagazineElement);
            reserve = root.Q<Label>(ReserveElement);
            shownMagazine = Hidden;
            shownReserve = Hidden;

            if (box == null || magazine == null || reserve == null)
            {
                Log.Error("Overlay document has no {} box, the ammo counter stays hidden", BoxElement);
                return false;
            }

            box.style.display = DisplayStyle.None;

            return true;
        }

        protected override void Unbind()
        {
            box = null;
            magazine = null;
            reserve = null;
        }

        private void Hide()
        {
            if (shownMagazine == Hidden && shownReserve == Hidden) return;

            shownMagazine = Hidden;
            shownReserve = Hidden;
            box.style.display = DisplayStyle.None;
        }

        private Inventory Own()
        {
            if (bag == null) bag = OwnPlayer.Find<Inventory>();

            return bag;
        }
    }
}
