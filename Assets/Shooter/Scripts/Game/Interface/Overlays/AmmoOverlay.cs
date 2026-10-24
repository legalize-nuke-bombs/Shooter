using Shooter.Infrastructure;
using UnityEngine.UIElements;

namespace Shooter.Game
{
    public class AmmoOverlay : Overlay
    {
        private const string CounterElementName = "ammo";
        private const string MagazineLabelName = "ammo-magazine";
        private const string ReserveLabelName = "ammo-reserve";
        private const int Hidden = -1;
        private static readonly Journal Log = Logs.Here();

        private Inventory inventory;
        private InventoryHolder inventoryHolder;

        private VisualElement counterElement;
        private Label magazineLabel;
        private Label reserveLabel;
        private int shownMagazineRounds = Hidden;
        private int shownReserveRounds = Hidden;

        private void Update()
        {
            if (!IsBound) return;

            if (inventory == null) inventory = LocalPlayerFinder.FindComponent<Inventory>();
            if (inventoryHolder == null) inventoryHolder = LocalPlayerFinder.FindComponent<InventoryHolder>();

            Firearm firearm = inventoryHolder == null ? null : inventoryHolder.HeldItem as Firearm;
            ItemCatalog itemCatalog = Catalogs.GetCatalog<ItemCatalog>();
            FirearmAsset firearmAsset = itemCatalog == null || firearm == null ? null : itemCatalog.GetFirearmAssetById(firearm.AssetId);

            if (firearm == null || firearmAsset == null)
            {
                HideCounter();
                return;
            }

            int magazineRounds = firearm.Magazine;
            int reserveRounds = firearmAsset.Ammo == null ? 0 : inventory.CountStackable(firearmAsset.Ammo);

            if (magazineRounds == shownMagazineRounds && reserveRounds == shownReserveRounds) return;

            shownMagazineRounds = magazineRounds;
            shownReserveRounds = reserveRounds;

            counterElement.style.display = DisplayStyle.Flex;
            magazineLabel.text = magazineRounds.ToString();
            reserveLabel.text = reserveRounds.ToString();
        }

        protected override bool TryBindElements(VisualElement root)
        {
            counterElement = root.Q<VisualElement>(CounterElementName);
            magazineLabel = root.Q<Label>(MagazineLabelName);
            reserveLabel = root.Q<Label>(ReserveLabelName);
            shownMagazineRounds = Hidden;
            shownReserveRounds = Hidden;

            if (counterElement == null || magazineLabel == null || reserveLabel == null)
            {
                Log.Error($"Overlay document has no {CounterElementName} box, the ammo counter stays hidden");
                return false;
            }

            counterElement.style.display = DisplayStyle.None;

            return true;
        }

        protected override void UnbindElements()
        {
            counterElement = null;
            magazineLabel = null;
            reserveLabel = null;
        }

        private void HideCounter()
        {
            if (shownMagazineRounds == Hidden && shownReserveRounds == Hidden) return;

            shownMagazineRounds = Hidden;
            shownReserveRounds = Hidden;
            counterElement.style.display = DisplayStyle.None;
        }
    }
}
