using System.Collections.Generic;
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
        private const string GridElement = "inventory-grid";
        private const string EmptyElement = "inventory-empty";
        private const string HeldElement = "inventory-held";
        private const string CoinsElement = "inventory-coins";
        private const string Coins = "coin";
        private const float Cell = 54f;
        private const int Columns = 10;
        private const int Rows = 8;

        private VisualElement window;
        private VisualElement grid;
        private VisualElement held;
        private Label empty;
        private Label coins;
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
            grid = root.Q<VisualElement>(GridElement);
            empty = root.Q<Label>(EmptyElement);
            held = root.Q<VisualElement>(HeldElement);
            coins = root.Q<Label>(CoinsElement);

            if (window == null || grid == null || empty == null || held == null || coins == null)
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
            grid?.Clear();
            held?.Clear();
            Log.Info("The bag is closed");
        }

        private void Touch()
        {
            stale = true;
        }

        private void Fill()
        {
            stale = false;
            grid.Clear();
            held.Clear();

            Paper();

            if (bag == null)
            {
                empty.style.display = DisplayStyle.Flex;
                coins.text = "0";
                return;
            }

            UniqueItem equipped = bag.Equipped();
            var taken = new bool[Rows, Columns];
            int money = 0;
            int packed = 0;

            if (equipped != null) held.Add(Held(equipped));

            foreach (UniqueItem item in bag.Uniques)
            {
                if (equipped != null && item.Id == equipped.Id) continue;

                ItemSpec spec = bag.Spec(item);
                if (Pack(taken, spec, out int row, out int column)) packed++;

                grid.Add(Thing(spec, item.SpecId, row, column, null, item.Id, spec != null && spec.Equipable));
            }

            foreach (StackRecord stack in bag.Stacks)
            {
                if (stack.SpecId == Coins)
                {
                    money += stack.Amount;
                    continue;
                }

                ItemSpec spec = bag.Spec(stack.SpecId);
                if (Pack(taken, spec, out int row, out int column)) packed++;

                grid.Add(Thing(spec, stack.SpecId.ToString(), row, column, stack.Amount.ToString(), Inventory.Nothing, false));
            }

            coins.text = money.ToString();
            empty.style.display = packed == 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void Paper()
        {
            grid.style.width = Columns * Cell;
            grid.style.height = Rows * Cell;

            for (int row = 0; row < Rows; row++)
            {
                for (int column = 0; column < Columns; column++)
                {
                    var cell = new VisualElement();
                    cell.AddToClassList("grid__cell");
                    cell.style.left = column * Cell;
                    cell.style.top = row * Cell;
                    cell.style.width = Cell;
                    cell.style.height = Cell;
                    grid.Add(cell);
                }
            }
        }

        private static bool Pack(bool[,] taken, ItemSpec spec, out int row, out int column)
        {
            Vector2Int size = spec == null ? Vector2Int.one : spec.Cells;

            for (row = 0; row + size.y <= Rows; row++)
            {
                for (column = 0; column + size.x <= Columns; column++)
                {
                    if (!Free(taken, row, column, size)) continue;

                    Fill(taken, row, column, size);

                    return true;
                }
            }

            row = 0;
            column = 0;

            return false;
        }

        private static bool Free(bool[,] taken, int row, int column, Vector2Int size)
        {
            for (int down = 0; down < size.y; down++)
            {
                for (int right = 0; right < size.x; right++)
                {
                    if (taken[row + down, column + right]) return false;
                }
            }

            return true;
        }

        private static void Fill(bool[,] taken, int row, int column, Vector2Int size)
        {
            for (int down = 0; down < size.y; down++)
            {
                for (int right = 0; right < size.x; right++) taken[row + down, column + right] = true;
            }
        }

        private VisualElement Thing(ItemSpec spec, string fallback, int row, int column, string amount, ulong id, bool equipable)
        {
            Vector2Int size = spec == null ? Vector2Int.one : spec.Cells;
            Button thing = Slot(spec, fallback, false, equipable);

            thing.style.position = Position.Absolute;
            thing.style.left = column * Cell;
            thing.style.top = row * Cell;
            thing.style.width = size.x * Cell;
            thing.style.height = size.y * Cell;

            if (amount != null)
            {
                var label = new Label(amount);
                label.AddToClassList("slot__amount");
                thing.Add(label);
            }

            if (equipable) thing.clicked += () => Equip(id, false);

            return thing;
        }

        private VisualElement Held(UniqueItem item)
        {
            ItemSpec spec = bag.Spec(item);
            Vector2Int size = spec == null ? Vector2Int.one : spec.Cells;
            Button thing = Slot(spec, item.SpecId, true, true);

            thing.style.width = size.x * Cell;
            thing.style.height = size.y * Cell;
            thing.clicked += () => Equip(item.Id, true);

            return thing;
        }

        private Button Slot(ItemSpec spec, string fallback, bool holding, bool equipable)
        {
            var slot = new Button { text = string.Empty, tooltip = spec == null ? fallback : spec.Title };
            slot.AddToClassList("slot");
            if (holding) slot.AddToClassList("slot--held");
            if (!equipable) slot.AddToClassList("slot--fixed");

            if (spec != null && spec.Icon != null)
            {
                var icon = new VisualElement();
                icon.AddToClassList("slot__icon");
                icon.style.backgroundImage = Background.FromSprite(spec.Icon);
                slot.Add(icon);
            }
            else
            {
                var name = new Label(spec == null ? fallback : spec.Title);
                name.AddToClassList("slot__name");
                slot.Add(name);
            }

            slot.SetEnabled(equipable);

            return slot;
        }

        private void Equip(ulong id, bool holding)
        {
            bag.EquipRpc(holding ? Inventory.Nothing : id);

            if (holding) return;

            OwnPlayer.Find<LocalPlayer>()?.CloseInventory();
        }
    }
}
