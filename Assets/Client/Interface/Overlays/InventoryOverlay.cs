using System;
using System.Collections.Generic;
using System.Linq;
using Shooter.Client.Playing;
using Shooter.Game.Core;
using Shooter.Game.Crafting;
using Shooter.Game.Loot;
using Shooter.Logging;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Client.Interface
{
    [RequireComponent(typeof(Aimer))]
    public class InventoryOverlay : Overlay
    {
        private const string WindowElement = "inventory-screen";
        private const string GridElement = "inventory-grid";
        private const string HeldElement = "inventory-held";
        private const string CoinsElement = "inventory-coins";
        private const string CraftGridElement = "craft-grid";
        private const string CraftOutputElement = "craft-output";
        private const int CraftSide = 3;
        private const string Coins = "coin";
        private const float Cell = 48f;
        private const float Bezel = 8f;
        private const int Columns = 10;
        private const int Rows = 6;
        private const int HandRows = 2;
        private static readonly Journal Log = Logs.Here();
        private readonly StackableItemSpec[] bench = new StackableItemSpec[CraftSide * CraftSide];
        private Aimer aimer;
        private Inventory bag;
        private Crafter crafter;
        private VisualElement craftGrid;
        private VisualElement craftOutput;
        private StackableItemSpec draggedStack;
        private Label coins;
        private VisualElement curtain;
        private int dragged;
        private bool draggedFromHands;
        private VisualElement ghost;
        private VisualElement grid;
        private VisualElement held;
        private bool open;
        private int pointer;
        private bool stale;

        private VisualElement window;

        private void Awake()
        {
            aimer = GetComponent<Aimer>();
        }

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
            held = root.Q<VisualElement>(HeldElement);
            coins = root.Q<Label>(CoinsElement);
            craftGrid = root.Q<VisualElement>(CraftGridElement);
            craftOutput = root.Q<VisualElement>(CraftOutputElement);

            if (window == null || grid == null || held == null || coins == null || craftGrid == null || craftOutput == null)
            {
                Log.Error($"Overlay document has no {WindowElement} window, the bag stays hidden");
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
            crafter = OwnPlayer.Find<Crafter>();

            if (bag != null) bag.Changed += Touch;

            window.style.display = DisplayStyle.Flex;
            stale = true;
            Log.Info("The bag is open");
        }

        private void Close()
        {
            CloseMenu();

            if (bag != null) bag.Changed -= Touch;
            bag = null;
            crafter = null;
            Array.Clear(bench, 0, bench.Length);

            if (window != null) window.style.display = DisplayStyle.None;
            grid?.Clear();
            held?.Clear();
            craftGrid?.Clear();
            craftOutput?.Clear();
            Log.Info("The bag is closed");
        }

        private void Touch()
        {
            stale = true;
        }

        private void Fill()
        {
            stale = false;
            CloseMenu();
            grid.Clear();
            held.Clear();

            Paper(held, HandRows);
            Paper(grid, Rows);

            if (bag == null)
            {
                coins.text = "0";
                return;
            }

            ItemCatalog catalog = Catalogs.Of<ItemCatalog>();
            UniqueItem equipped = bag.Equipped();
            int equippedSlot = bag.EquippedSlot;
            bool[,] taken = new bool[Rows, Columns];
            int money = 0;

            if (equipped != null)
            {
                ItemSpec spec = catalog == null ? null : catalog.Spec(equipped.SpecId);

                VisualElement thing = Thing(spec, equipped.SpecId, 0, 0, null, equippedSlot, true, true);
                AddMenu(thing, null, 0, equippedSlot);
                held.Add(thing);
            }

            IReadOnlyList<UniqueItem> items = bag.UniqueItems;

            for (int slot = 0; slot < items.Count; slot++)
            {
                UniqueItem item = items[slot];
                if (item == null || slot == equippedSlot) continue;

                ItemSpec spec = catalog == null ? null : catalog.Spec(item.SpecId);
                Pack(taken, spec, out int row, out int column);

                VisualElement thing = Thing(spec, item.SpecId, row, column, null, slot,
                    spec is UniqueItemSpec unique && unique.Equipable, false);
                AddMenu(thing, null, 0, slot);
                grid.Add(thing);
            }

            int kinds = catalog == null ? 0 : catalog.Count;

            for (int index = 0; index < kinds; index++)
            {
                if (catalog.At(index) is not StackableItemSpec spec) continue;

                int amount = bag.StackableAmount(spec);
                if (amount == 0) continue;

                if (spec.Key == Coins)
                {
                    money += amount;
                    continue;
                }

                Pack(taken, spec, out int row, out int column);

                VisualElement thing = Thing(spec, spec.Key, row, column, amount.ToString(), Inventory.NoSlot, false,
                    false, spec);
                AddMenu(thing, spec, amount, Inventory.NoSlot);

                grid.Add(thing);
            }

            coins.text = money.ToString();
            Bench();
        }

        // The bench is a picture of what the player wants to combine: the stacks stay in the bag, the server
        // takes them at the craft; a recipe matches by amounts, like the residents describe it
        private void Bench()
        {
            craftGrid.Clear();
            craftOutput.Clear();

            Paper(craftGrid, CraftSide, CraftSide);
            Paper(craftOutput, 1, 1);

            for (int i = 0; i < bench.Length; i++)
            {
                StackableItemSpec spec = bench[i];
                if (spec == null) continue;

                int cell = i;
                VisualElement thing = Placed(spec, i / CraftSide, i % CraftSide);
                thing.RegisterCallback<PointerDownEvent>(down =>
                {
                    if (down.button != 0 || ghost != null) return;

                    bench[cell] = null;
                    Bench();
                    down.StopPropagation();
                });
                craftGrid.Add(thing);
            }

            Craft match = Match();
            if (match == null) return;

            VisualElement output = Placed(match.Output, 0, 0);
            output.RegisterCallback<PointerDownEvent>(down =>
            {
                if (down.button != 0 || ghost != null || bag == null) return;

                bag.CraftRpc(match.Id);
                down.StopPropagation();
            });
            craftOutput.Add(output);
        }

        private static VisualElement Placed(ItemSpec spec, int row, int column)
        {
            VisualElement thing = Slot(spec, spec.Key, false, true);
            thing.style.position = Position.Absolute;
            thing.style.left = column * Cell;
            thing.style.top = row * Cell;
            thing.style.width = Cell;
            thing.style.height = Cell;

            return thing;
        }

        // A known recipe whose amounts equal the bench and whose ingredients the bag can pay
        private Craft Match()
        {
            if (crafter == null || bag == null) return null;

            var wanted = new Dictionary<StackableItemSpec, int>();
            foreach (StackableItemSpec spec in bench)
            {
                if (spec == null) continue;

                wanted.TryAdd(spec, 0);
                wanted[spec]++;
            }

            if (wanted.Count == 0) return null;

            foreach (Craft craft in crafter.AvailableCrafts)
            {
                if (craft == null || craft.Output == null) continue;

                Dictionary<StackableItemSpec, int> needed = craft.AmountMap();
                if (needed.Count != wanted.Count) continue;
                if (!needed.All(pair => wanted.TryGetValue(pair.Key, out int amount) && amount == pair.Value)) continue;
                if (!needed.All(pair => bag.StackableAmount(pair.Key) >= pair.Value)) continue;

                return craft;
            }

            return null;
        }

        private int BenchCellAt(Vector2 at)
        {
            Rect bounds = craftGrid.worldBound;
            if (!bounds.Contains(at)) return -1;

            int column = Mathf.Clamp((int)((at.x - bounds.x) / Cell), 0, CraftSide - 1);
            int row = Mathf.Clamp((int)((at.y - bounds.y) / Cell), 0, CraftSide - 1);

            return row * CraftSide + column;
        }

        private static void Paper(VisualElement host, int rows)
        {
            Paper(host, rows, Columns);
        }

        private static void Paper(VisualElement host, int rows, int columns)
        {
            host.style.width = columns * Cell;
            host.style.height = rows * Cell;

            for (int row = 0; row < rows; row++)
            for (int column = 0; column < columns; column++)
            {
                var cell = new VisualElement();
                cell.AddToClassList("grid__cell");
                cell.style.left = column * Cell;
                cell.style.top = row * Cell;
                cell.style.width = Cell;
                cell.style.height = Cell;
                host.Add(cell);
            }
        }

        private static bool Pack(bool[,] taken, ItemSpec spec, out int row, out int column)
        {
            Vector2Int size = spec == null ? Vector2Int.one : spec.Cells;

            for (row = 0; row + size.y <= Rows; row++)
            for (column = 0; column + size.x <= Columns; column++)
            {
                if (!Free(taken, row, column, size)) continue;

                Fill(taken, row, column, size);

                return true;
            }

            row = 0;
            column = 0;

            return false;
        }

        private static bool Free(bool[,] taken, int row, int column, Vector2Int size)
        {
            for (int down = 0; down < size.y; down++)
            for (int right = 0; right < size.x; right++)
                if (taken[row + down, column + right])
                    return false;

            return true;
        }

        private static void Fill(bool[,] taken, int row, int column, Vector2Int size)
        {
            for (int down = 0; down < size.y; down++)
            for (int right = 0; right < size.x; right++)
                taken[row + down, column + right] = true;
        }

        private VisualElement Thing(ItemSpec spec, string fallback, int row, int column, string amount, int slot,
            bool equipable, bool holding, StackableItemSpec stack = null)
        {
            Vector2Int cells = spec == null ? Vector2Int.one : spec.Cells;
            var size = new Vector2(cells.x * Cell, cells.y * Cell);
            VisualElement thing = Slot(spec, fallback, holding, equipable);

            thing.style.position = Position.Absolute;
            thing.style.left = column * Cell;
            thing.style.top = row * Cell;
            thing.style.width = size.x;
            thing.style.height = size.y;

            if (amount != null)
            {
                var label = new Label(amount);
                label.AddToClassList("slot__amount");
                thing.Add(label);
            }

            if (equipable || stack != null) Draggable(thing, slot, holding, stack, Icon(spec), size);

            return thing;
        }

        private static Sprite Icon(ItemSpec spec)
        {
            return spec == null || spec.Icon == null ? null : spec.Icon.Sprite;
        }

        private void Draggable(VisualElement thing, int slot, bool holding, StackableItemSpec stack, Sprite icon,
            Vector2 size)
        {
            thing.RegisterCallback<PointerDownEvent>(down =>
            {
                if (down.button != 0 || ghost != null) return;

                dragged = slot;
                draggedFromHands = holding;
                draggedStack = stack;
                pointer = down.pointerId;

                ghost = Ghost(icon, size);
                window.Add(ghost);
                Follow(down.position, size);

                thing.CapturePointer(pointer);
                down.StopPropagation();
            });

            thing.RegisterCallback<PointerMoveEvent>(move =>
            {
                if (ghost == null || move.pointerId != pointer) return;

                Follow(move.position, size);
            });

            thing.RegisterCallback<PointerUpEvent>(up =>
            {
                if (ghost == null || up.pointerId != pointer) return;

                thing.ReleasePointer(pointer);
                Drop(up.position);
            });
        }

        private static VisualElement Ghost(Sprite icon, Vector2 size)
        {
            var shadow = new VisualElement();
            shadow.AddToClassList("ghost");
            shadow.style.width = size.x;
            shadow.style.height = size.y;

            if (icon != null) shadow.style.backgroundImage = Background.FromSprite(icon);

            return shadow;
        }

        private void Follow(Vector2 at, Vector2 size)
        {
            ghost.style.left = at.x - size.x / 2f;
            ghost.style.top = at.y - size.y / 2f;
        }

        private void Drop(Vector2 at)
        {
            ghost.RemoveFromHierarchy();
            ghost = null;

            if (bag == null) return;

            // A stack goes onto the bench, a unique between the hands and the bag
            if (draggedStack != null)
            {
                int cell = BenchCellAt(at);
                if (cell >= 0)
                {
                    bench[cell] = draggedStack;
                    Bench();
                }

                draggedStack = null;
                return;
            }

            if (held.worldBound.Contains(at) && !draggedFromHands) bag.EquipRpc(dragged);
            else if (grid.worldBound.Contains(at) && draggedFromHands) bag.EquipRpc(Inventory.NoSlot);
        }

        // A stack comes with its spec and amount, a unique with its slot
        private void AddMenu(VisualElement thing, StackableItemSpec stack, int amount, int slot)
        {
            thing.RegisterCallback<PointerDownEvent>(down =>
            {
                if (down.button != 1 || ghost != null) return;

                OpenMenu(down.position, stack, amount, slot);
                down.StopPropagation();
            });
        }

        private void OpenMenu(Vector2 at, StackableItemSpec stack, int amount, int slot)
        {
            CloseMenu();

            curtain = new VisualElement();
            curtain.AddToClassList("menu-curtain");
            curtain.RegisterCallback<PointerDownEvent>(down =>
            {
                if (down.target == curtain) CloseMenu();
            });

            var menu = new VisualElement();
            menu.AddToClassList("context-menu");

            Vector2 local = window.WorldToLocal(at);
            menu.style.left = local.x;
            menu.style.top = local.y;

            if (stack != null && stack.Usable) Item(menu, "Использовать", () => bag.UseStackableRpc(stack.Id));

            // Giving is offered only while somebody stands in the crosshair; the server checks the reach again
            Character taker = Aimed();
            if (taker != null)
            {
                long takerId = taker.Id;

                if (stack != null) Item(menu, "Отдать", () => bag.GiveStackableRpc(takerId, stack.Id, 1));
                if (stack != null && amount > 1)
                    Item(menu, "Отдать все", () => bag.GiveStackableRpc(takerId, stack.Id, amount));
                if (stack == null) Item(menu, "Отдать", () => bag.GiveUniqueRpc(takerId, slot));
            }

            if (menu.childCount == 0) return;

            curtain.Add(menu);
            window.Add(curtain);
        }

        private void Item(VisualElement menu, string text, Action action)
        {
            var item = new Button(() =>
            {
                if (bag != null) action();

                CloseMenu();
            }) { text = text };
            item.AddToClassList("context-menu__item");
            menu.Add(item);
        }

        private Character Aimed()
        {
            if (!aimer.TryHit(out RaycastHit hit) || hit.collider == null) return null;

            return hit.collider.GetComponentInParent<Character>();
        }

        private void CloseMenu()
        {
            if (curtain == null) return;

            curtain.RemoveFromHierarchy();
            curtain = null;
        }

        private static VisualElement Slot(ItemSpec spec, string fallback, bool holding, bool equipable)
        {
            Vector2Int size = spec == null ? Vector2Int.one : spec.Cells;

            var slot = new VisualElement { tooltip = spec == null ? fallback : spec.Title };
            slot.AddToClassList("slot");
            if (holding) slot.AddToClassList("slot--held");
            if (!equipable) slot.AddToClassList("slot--fixed");

            if (Icon(spec) != null)
            {
                var icon = new VisualElement();
                icon.AddToClassList("slot__icon");
                icon.style.backgroundImage = Background.FromSprite(Icon(spec));
                icon.style.width = size.x * Cell - Bezel;
                icon.style.height = size.y * Cell - Bezel;
                slot.Add(icon);
            }
            else
            {
                var name = new Label(spec == null ? fallback : spec.Title);
                name.AddToClassList("slot__name");
                slot.Add(name);
            }

            return slot;
        }
    }
}
