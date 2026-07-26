using UnityEngine;
using UnityEngine.UIElements;
using Shooter.Client.Playing;
using Shooter.Game.Loot;
using Shooter.Logging;

namespace Shooter.Client.Interface.Overlays
{
    [RequireComponent(typeof(PanelRenderer))]
    public class InventoryOverlay : MonoBehaviour
    {
        private const string WindowElement = "inventory";
        private const string SlotsElement = "inventory-slots";
        private const string EmptyElement = "inventory-empty";

        [SerializeField] private ItemNameCatalog names;

        private PanelRenderer panel;
        private VisualElement window;
        private VisualElement rows;
        private Label empty;
        private Inventory bag;
        private bool open;
        private bool stale;

        private void OnEnable()
        {
            panel = GetComponent<PanelRenderer>();
            panel.RegisterUIReloadCallback(Bind);
        }

        private void OnDisable()
        {
            panel.UnregisterUIReloadCallback(Bind);

            if (open) Close();

            window = null;
        }

        private void Update()
        {
            if (window == null) return;

            LocalPlayer player = OwnPlayer.Find<LocalPlayer>();
            bool wanted = player != null && player.InventoryOpen;

            if (wanted != open)
            {
                open = wanted;

                if (open) Open();
                else Close();
            }

            if (open && stale) Fill();
        }

        private void Bind(PanelRenderer renderer, VisualElement root)
        {
            if (open) Close();

            window = root.Q<VisualElement>(WindowElement);
            rows = root.Q<VisualElement>(SlotsElement);
            empty = root.Q<Label>(EmptyElement);

            if (window == null || rows == null || empty == null)
            {
                Log.Error("Overlay document has no {} window, the bag stays hidden", WindowElement);
                window = null;
                return;
            }

            if (names == null)
            {
                Log.Error("Inventory overlay has no item name catalog, the bag stays hidden");
                window = null;
                return;
            }

            window.style.display = DisplayStyle.None;
        }

        private void Open()
        {
            bag = OwnPlayer.Find<Inventory>();

            if (bag != null) bag.Changed += Touch;

            window.style.display = DisplayStyle.Flex;
            stale = true;
            Log.Info("The bag is open");
        }

        private void Close()
        {
            if (bag != null) bag.Changed -= Touch;
            bag = null;

            if (window != null) window.style.display = DisplayStyle.None;
            rows?.Clear();
            Log.Info("The bag is closed");
        }

        private void Touch()
        {
            stale = true;
        }

        private void Fill()
        {
            stale = false;
            rows.Clear();

            int count = bag == null ? 0 : bag.Count;
            empty.style.display = count == 0 ? DisplayStyle.Flex : DisplayStyle.None;

            for (int slot = 0; slot < count; slot++)
                rows.Add(Row(slot, bag.At(slot)));
        }

        private VisualElement Row(int slot, Item item)
        {
            bool held = slot == bag.EquippedSlot;
            bool equipable = bag.Equipable(item);

            var row = new Button { text = string.Empty };
            row.AddToClassList("slot");
            if (held) row.AddToClassList("slot--held");
            if (!equipable) row.AddToClassList("slot--fixed");

            var name = new Label(names.Text(item.Type));
            name.AddToClassList("slot__name");
            row.Add(name);

            var amount = new Label(Amount(item));
            amount.AddToClassList("slot__amount");
            row.Add(amount);

            row.SetEnabled(equipable);
            if (equipable) row.clicked += () => bag.EquipRpc(held ? Inventory.Nothing : slot);

            return row;
        }

        private string Amount(Item item)
        {
            ItemSpec spec = bag.Catalog == null ? null : bag.Catalog.Spec(item.Type);

            return spec != null && spec.Stackable ? item.Amount.ToString() : string.Empty;
        }
    }
}
