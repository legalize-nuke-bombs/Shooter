using Shooter.Infrastructure;
using UnityEngine.UIElements;

namespace Shooter.Game
{
    public class AmmoOverlay : Overlay
    {
        private const string BoxElement = "ammo";
        private const string MagazineElement = "ammo-magazine";
        private const string ReserveElement = "ammo-reserve";
        private const int Hidden = -1;
        private static readonly Journal Log = Logs.CreateJournalForThisFile();
        private Inventory bag;
        private InventoryHolder holder;

        private VisualElement box;
        private Label magazine;
        private Label reserve;
        private int shownMagazine = Hidden;
        private int shownReserve = Hidden;

        private void Update()
        {
            if (!Bound) return;

            if (bag == null) bag = OwnPlayer.Find<Inventory>();
            if (holder == null) holder = OwnPlayer.Find<InventoryHolder>();

            Firearm firearm = holder == null ? null : holder.HeldItem as Firearm;
            ItemCatalog items = Catalogs.GetCatalog<ItemCatalog>();
            FirearmAsset asset = items == null || firearm == null ? null : items.GetFirearmAssetById(firearm.AssetId);

            if (firearm == null || asset == null)
            {
                Hide();
                return;
            }

            int held = firearm.RoundsInMagazine;
            int left = asset.Ammo == null ? 0 : bag.CountStackable(asset.Ammo);

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
                Log.Error($"Overlay document has no {BoxElement} box, the ammo counter stays hidden");
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

    }
}
