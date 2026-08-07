using Shooter.Client.Playing;
using Shooter.Game.Loot;
using Shooter.Logging;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Client.Interface.Overlays
{
    public class InventoryOverlay : Overlay
    {
        private static readonly Journal Log = Logs.Here();

        private const string WindowElement = "inventory";
        private const string SlotsElement = "inventory-slots";
        private const string EmptyElement = "inventory-empty";

        private VisualElement window;
        private VisualElement rows;
        private Label empty;
        private Inventory bag;
        private bool open;
        private bool stale;

        private void Update()
        {
            if (!Bound) return;

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

        protected override bool Bind(VisualElement root)
        {
            window = root.Q<VisualElement>(WindowElement);
            rows = root.Q<VisualElement>(SlotsElement);
            empty = root.Q<Label>(EmptyElement);

            if (window == null || rows == null || empty == null)
            {
                Log.Error("Overlay document has no {} window, the bag stays hidden", WindowElement);
                return false;
            }

            window.style.display = DisplayStyle.None;

            return true;
        }

        protected override void Unbind()
        {
            if (open) Close();

            open = false;
            window = null;
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

            if (bag == null) return;

            foreach (StackRecord stack in bag.Stacks) rows.Add(Row(stack));

            foreach (UniqueItem item in bag.Uniques) rows.Add(Row(item));
        }

        private VisualElement Row(StackRecord stack)
        {
            ItemSpec spec = bag.Spec(stack.SpecId);
            VisualElement row = Slot(spec == null ? stack.SpecId.ToString() : spec.Title, false, false);

            var amount = new Label(stack.Amount.ToString());
            amount.AddToClassList("slot__amount");
            row.Add(amount);

            return row;
        }

        private VisualElement Row(UniqueItem item)
        {
            bool held = item.Id == bag.EquippedId;
            ItemSpec spec = bag.Spec(item);
            bool equipable = spec != null && spec.Equipable;

            VisualElement row = Slot(spec == null ? item.SpecId : spec.Title, held, equipable);

            if (equipable) ((Button)row).clicked += () => Equip(item.Id, held);

            return row;
        }

        private VisualElement Slot(string title, bool held, bool equipable)
        {
            var row = new Button { text = string.Empty };
            row.AddToClassList("slot");
            if (held) row.AddToClassList("slot--held");
            if (!equipable) row.AddToClassList("slot--fixed");

            var name = new Label(title);
            name.AddToClassList("slot__name");
            row.Add(name);

            row.SetEnabled(equipable);

            return row;
        }

        private void Equip(ulong id, bool held)
        {
            bag.EquipRpc(held ? Inventory.Nothing : id);

            if (held) return;

            OwnPlayer.Find<LocalPlayer>()?.CloseInventory();
        }
    }
}
