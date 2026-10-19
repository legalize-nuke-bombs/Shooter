using System;
using System.Collections.Generic;
using Shooter.Infrastructure;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Game
{
    public class InventoryOverlay : Overlay
    {
        private const string WindowElement = "inventory-screen";
        private const string GridElement = "inventory-grid";
        private const string HeldElement = "inventory-held";
        private const string CraftGridElement = "craft-grid";
        private const string CraftOutputElement = "craft-output";
        private const string GiveElement = "give";
        private const string GiveTitleElement = "give-title";
        private const string GiveGridElement = "give-grid";
        private const string GiveButtonElement = "give-button";
        private const int CraftSide = 3;
        private const int GiveColumns = 4;
        private const int GiveRows = 3;
        private const float Cell = 48f;
        private const float Bezel = 8f;
        private const int Columns = 10;
        private const int Rows = 6;
        private const int HandRows = 2;
        private const float ConeAngle = 12f;
        private const float BodyRadius = 0.6f;
        private const float SendPatience = 3f;
        private const float LookInterval = 0.2f;
        private const float Stickiness = 1.25f;
        private const int SplitDigits = 7;
        private static readonly Journal Log = Logs.CreateJournalForThisFile();

        // Points along a body from feet to head around its root, which sits at the middle of the body for players and residents alike
        private static readonly float[] BodyHeights = { -0.8f, -0.4f, 0f, 0.4f, 0.8f };

        private struct Unit
        {
            public StackableItemAsset Asset;
            public int Count;
        }

        private struct Offer
        {
            public ItemAsset Asset;
            public string Key;
            public int Count;
            public int Slot;
            public int Expected;
            public float SentAt;

            public StackableItemAsset Stack => Slot == Inventory.NoSlot ? Asset as StackableItemAsset : null;
        }

        private readonly Unit[] bench = new Unit[CraftSide * CraftSide];
        private readonly List<Unit> splits = new();
        private readonly List<Offer> offers = new();
        private readonly NameMapper names = new();
        private readonly RaycastHit[] sights = new RaycastHit[16];
        private Inventory bag;
        private InventoryHolder holder;
        private InventoryExchanger exchanger;
        private InventoryUser user;
        private Crafter crafter;
        private Character own;
        private Character taker;
        private float nextLook;
        private VisualElement craftGrid;
        private VisualElement craftOutput;
        private VisualElement give;
        private Label giveTitle;
        private VisualElement giveGrid;
        private Button giveButton;
        private StackableItemAsset draggedStack;
        private int draggedSplit = -1;
        private int draggedCell = -1;
        private int draggedOffer = -1;
        private bool draggedOutput;
        private bool draggedUnique;
        private bool draggedEquipable;
        private Craft pendingCraft;
        private int outputBefore;
        private VisualElement curtain;
        private VisualElement prompt;
        private int dragged;
        private bool draggedFromHands;
        private VisualElement ghost;
        private VisualElement grid;
        private VisualElement held;
        private bool open;
        private int pointer;
        private bool stale;

        private VisualElement window;

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

            if (!open) return;

            if (prompt != null && (player == null || !player.Prompting)) HidePrompt();

            Watch();
            if (stale) Fill();
        }

        protected override bool Bind(VisualElement root)
        {
            window = root.Q<VisualElement>(WindowElement);
            grid = root.Q<VisualElement>(GridElement);
            held = root.Q<VisualElement>(HeldElement);
            craftGrid = root.Q<VisualElement>(CraftGridElement);
            craftOutput = root.Q<VisualElement>(CraftOutputElement);
            give = root.Q<VisualElement>(GiveElement);
            giveTitle = root.Q<Label>(GiveTitleElement);
            giveGrid = root.Q<VisualElement>(GiveGridElement);
            giveButton = root.Q<Button>(GiveButtonElement);

            if (window == null || grid == null || held == null || craftGrid == null || craftOutput == null ||
                give == null || giveTitle == null || giveGrid == null || giveButton == null)
            {
                Log.Error($"Overlay document has no {WindowElement} window, the bag stays hidden");
                return false;
            }

            window.style.display = DisplayStyle.None;
            give.style.display = DisplayStyle.None;
            giveButton.clicked += HandOver;

            return true;
        }

        protected override void Unbind()
        {
            if (open) Close();

            if (giveButton != null) giveButton.clicked -= HandOver;

            open = false;
            window = null;
        }

        private void Open()
        {
            bag = OwnPlayer.Find<Inventory>();
            holder = OwnPlayer.Find<InventoryHolder>();
            exchanger = OwnPlayer.Find<InventoryExchanger>();
            user = OwnPlayer.Find<InventoryUser>();
            crafter = OwnPlayer.Find<Crafter>();
            own = OwnPlayer.Find<Character>();
            taker = Taker(null);
            nextLook = Time.unscaledTime + LookInterval;

            if (bag != null) bag.Changed += Touch;
            if (holder != null) holder.Changed += Touch;

            window.style.display = DisplayStyle.Flex;
            stale = true;
            Log.Info(taker == null ? "The bag is open" : $"The bag is open, {taker.name} can take things from it");
        }

        private void Close()
        {
            CloseMenu();
            EndPrompt();

            if (bag != null) bag.Changed -= Touch;
            if (holder != null) holder.Changed -= Touch;
            bag = null;
            holder = null;
            exchanger = null;
            user = null;
            crafter = null;
            own = null;
            taker = null;
            pendingCraft = null;
            Array.Clear(bench, 0, bench.Length);
            splits.Clear();
            offers.Clear();

            if (window != null) window.style.display = DisplayStyle.None;
            if (give != null) give.style.display = DisplayStyle.None;
            grid?.Clear();
            held?.Clear();
            craftGrid?.Clear();
            craftOutput?.Clear();
            giveGrid?.Clear();
            Log.Info("The bag is closed");
        }

        private void Touch()
        {
            stale = true;
        }

        private void Watch()
        {
            if (Time.unscaledTime >= nextLook)
            {
                nextLook = Time.unscaledTime + LookInterval;
                Look();
            }

            if (taker == null) return;

            bool ready = false;
            for (int i = 0; i < offers.Count; i++)
            {
                Offer offer = offers[i];
                if (offer.SentAt <= 0f)
                {
                    ready = true;
                    continue;
                }

                if (Time.unscaledTime - offer.SentAt <= SendPatience) continue;

                Log.Info($"The bag never let {offer.Key} x {offer.Count} go, it is back in the offer");
                offer.SentAt = 0f;
                offers[i] = offer;
                stale = true;
            }

            bool near = own != null && exchanger != null &&
                        Vector3.Distance(own.transform.position, taker.transform.position) <= exchanger.Radius;
            giveButton.SetEnabled(near && ready);
        }

        private void Fill()
        {
            stale = false;
            CloseMenu();
            Settle();
            grid.Clear();
            held.Clear();

            Paper(held, HandRows);
            Paper(grid, Rows);

            if (bag == null) return;

            ItemCatalog catalog = Catalogs.GetCatalog<ItemCatalog>();
            UniqueItem equipped = holder.Held;
            int equippedSlot = holder.Slot;
            bool[,] taken = new bool[Rows, Columns];

            if (equipped != null && !Offered(equippedSlot))
            {
                ItemAsset asset = catalog == null ? null : catalog.Asset(equipped.AssetId);

                VisualElement thing = Thing(asset, equipped.AssetId, 0, 0, null, equippedSlot, true, true);
                AddMenu(thing, null, 0, equippedSlot);
                held.Add(thing);
            }

            IReadOnlyList<UniqueItem> items = bag.UniqueItems;

            for (int slot = 0; slot < items.Count; slot++)
            {
                UniqueItem item = items[slot];
                if (item == null || slot == equippedSlot || Offered(slot)) continue;

                ItemAsset asset = catalog == null ? null : catalog.Asset(item.AssetId);
                Pack(taken, asset, Rows, Columns, out int row, out int column);

                VisualElement thing = Thing(asset, item.AssetId, row, column, null, slot,
                    asset is UniqueItemAsset unique && unique.Equipable, false);
                AddMenu(thing, null, 0, slot);
                grid.Add(thing);
            }

            int kinds = catalog == null ? 0 : catalog.Count;

            for (int index = 0; index < kinds; index++)
            {
                if (catalog.GetAssetAt(index) is not StackableItemAsset asset) continue;

                int amount = bag.Count(asset) - Reserved(asset);
                if (amount > 0)
                {
                    Pack(taken, asset, Rows, Columns, out int row, out int column);

                    VisualElement thing = Thing(asset, asset.Id, row, column, amount.ToString(), Inventory.NoSlot,
                        false, false, asset);
                    AddMenu(thing, asset, amount, Inventory.NoSlot);
                    grid.Add(thing);
                }

                for (int i = 0; i < splits.Count; i++)
                {
                    if (splits[i].Asset != asset) continue;

                    Pack(taken, asset, Rows, Columns, out int row, out int column);
                    grid.Add(Thing(asset, asset.Id, row, column, splits[i].Count.ToString(), Inventory.NoSlot, false,
                        false, asset, i));
                }
            }

            Bench();
            Offers();
        }

        private int Placed(StackableItemAsset asset)
        {
            int placed = 0;
            foreach (Unit unit in bench)
                if (unit.Asset == asset)
                    placed += unit.Count;

            return placed;
        }

        private int Reserved(StackableItemAsset asset)
        {
            int reserved = Placed(asset);

            foreach (Unit split in splits)
                if (split.Asset == asset)
                    reserved += split.Count;

            foreach (Offer offer in offers)
                if (offer.Stack == asset)
                    reserved += offer.Count;

            return reserved;
        }

        private bool Offered(int slot)
        {
            foreach (Offer offer in offers)
                if (offer.Slot != Inventory.NoSlot && offer.Slot == slot)
                    return true;

            return false;
        }

        // A result that reached the bag pays one unit from every cell; an offer the bag let go is done;
        // what the bag no longer backs leaves the splits first, then the offer, then the bench
        private void Settle()
        {
            if (bag == null)
            {
                Array.Clear(bench, 0, bench.Length);
                splits.Clear();
                offers.Clear();
                return;
            }

            if (pendingCraft != null && Counted(pendingCraft.Output) > outputBefore)
            {
                for (int i = 0; i < bench.Length; i++)
                    if (bench[i].Asset != null)
                        Take(i, 1);

                pendingCraft = null;
            }

            IReadOnlyList<UniqueItem> items = bag.UniqueItems;

            for (int i = offers.Count - 1; i >= 0; i--)
            {
                Offer offer = offers[i];
                StackableItemAsset stack = offer.Stack;

                bool done = stack == null
                    ? offer.Slot >= items.Count || items[offer.Slot] == null || items[offer.Slot].AssetId != offer.Key
                    : offer.SentAt > 0f && bag.Count(stack) <= offer.Expected;

                if (done) offers.RemoveAt(i);
            }

            for (int i = splits.Count - 1; i >= 0; i--)
            {
                Unit split = splits[i];
                int over = Reserved(split.Asset) - bag.Count(split.Asset);
                if (over <= 0) continue;

                split.Count -= Math.Min(over, split.Count);
                if (split.Count <= 0) splits.RemoveAt(i);
                else splits[i] = split;
            }

            for (int i = offers.Count - 1; i >= 0; i--)
            {
                Offer offer = offers[i];
                StackableItemAsset stack = offer.Stack;
                if (stack == null || offer.SentAt > 0f) continue;

                int over = Reserved(stack) - bag.Count(stack);
                if (over <= 0) continue;

                offer.Count -= Math.Min(over, offer.Count);
                if (offer.Count <= 0) offers.RemoveAt(i);
                else offers[i] = offer;
            }

            for (int i = bench.Length - 1; i >= 0; i--)
            {
                StackableItemAsset asset = bench[i].Asset;
                if (asset == null) continue;

                int over = Reserved(asset) - bag.Count(asset);
                if (over > 0) Take(i, Math.Min(over, bench[i].Count));
            }
        }

        private void Take(int cell, int count)
        {
            bench[cell].Count -= count;
            if (bench[cell].Count <= 0) bench[cell] = default;
        }

        private int Counted(ItemAsset asset)
        {
            if (bag == null) return 0;
            if (asset is StackableItemAsset stackable) return bag.Count(stackable);

            int uniques = 0;
            foreach (UniqueItem item in bag.UniqueItems)
                if (item != null)
                    uniques++;

            return uniques;
        }

        private void Bench()
        {
            craftGrid.Clear();
            craftOutput.Clear();

            Paper(craftGrid, CraftSide, CraftSide);
            Paper(craftOutput, 1, 1);

            for (int i = 0; i < bench.Length; i++)
            {
                Unit unit = bench[i];
                if (unit.Asset == null) continue;

                int cell = i;
                VisualElement thing = Standing(unit.Asset, i / CraftSide, i % CraftSide);
                if (unit.Count > 1) Amount(thing, unit.Count);

                Draggable(thing, Icon(unit.Asset), new Vector2(Cell, Cell), () => draggedCell = cell);
                thing.RegisterCallback<PointerDownEvent>(down =>
                {
                    if (down.button != 1 || ghost != null) return;

                    bench[cell] = default;
                    stale = true;
                    down.StopPropagation();
                });
                craftGrid.Add(thing);
            }

            Craft match = Match();
            craftOutput.EnableInClassList("craft__output--ready", match != null);
            if (match == null) return;

            VisualElement output = Standing(match.Output, 0, 0);
            Draggable(output, Icon(match.Output), new Vector2(Cell, Cell), () => draggedOutput = true);
            craftOutput.Add(output);
        }

        private void Offers()
        {
            giveGrid.Clear();
            give.style.display = taker == null ? DisplayStyle.None : DisplayStyle.Flex;
            if (taker == null) return;

            string named = names.Of(taker.Id);
            giveTitle.text = string.IsNullOrEmpty(named) ? "Передать" : $"Передать: {named}";
            Paper(giveGrid, GiveRows, GiveColumns);

            bool[,] taken = new bool[GiveRows, GiveColumns];

            for (int i = 0; i < offers.Count; i++)
            {
                Offer offer = offers[i];
                Pack(taken, offer.Asset, GiveRows, GiveColumns, out int row, out int column);

                Vector2Int cells = offer.Asset == null ? Vector2Int.one : offer.Asset.Cells;
                var size = new Vector2(cells.x * Cell, cells.y * Cell);
                VisualElement thing = Slot(offer.Asset, offer.Key, false, offer.SentAt <= 0f);
                thing.style.position = Position.Absolute;
                thing.style.left = column * Cell;
                thing.style.top = row * Cell;
                thing.style.width = size.x;
                thing.style.height = size.y;

                if (offer.Stack != null) Amount(thing, offer.Count);

                if (offer.SentAt > 0f)
                {
                    thing.AddToClassList("slot--sent");
                    giveGrid.Add(thing);
                    continue;
                }

                int index = i;
                Draggable(thing, Icon(offer.Asset), size, () => draggedOffer = index);
                thing.RegisterCallback<PointerDownEvent>(down =>
                {
                    if (down.button != 1 || ghost != null) return;

                    if (index < offers.Count) offers.RemoveAt(index);
                    stale = true;
                    down.StopPropagation();
                });
                giveGrid.Add(thing);
            }
        }

        private bool Propose(StackableItemAsset asset, int amount)
        {
            for (int i = 0; i < offers.Count; i++)
            {
                Offer offer = offers[i];
                if (offer.Stack != asset || offer.SentAt > 0f) continue;

                offer.Count += amount;
                offers[i] = offer;
                return true;
            }

            if (!Fits(asset)) return false;

            offers.Add(new Offer { Asset = asset, Key = asset.Id, Count = amount, Slot = Inventory.NoSlot });
            return true;
        }

        private bool Propose(int slot)
        {
            if (bag == null || Offered(slot)) return false;

            IReadOnlyList<UniqueItem> items = bag.UniqueItems;
            UniqueItem item = slot >= 0 && slot < items.Count ? items[slot] : null;
            if (item == null) return false;

            ItemCatalog catalog = Catalogs.GetCatalog<ItemCatalog>();
            ItemAsset asset = catalog == null ? null : catalog.Asset(item.AssetId);
            if (!Fits(asset)) return false;

            offers.Add(new Offer { Asset = asset, Key = item.AssetId, Count = 1, Slot = slot });
            return true;
        }

        private bool Fits(ItemAsset extra)
        {
            bool[,] taken = new bool[GiveRows, GiveColumns];

            foreach (Offer offer in offers)
                if (!Pack(taken, offer.Asset, GiveRows, GiveColumns, out _, out _))
                    return false;

            return Pack(taken, extra, GiveRows, GiveColumns, out _, out _);
        }

        // The bag only lets go once the server confirms; until then the offer stays greyed out where it was
        private void HandOver()
        {
            if (bag == null || taker == null) return;

            long takerId = taker.Id;
            int sent = 0;

            for (int i = 0; i < offers.Count; i++)
            {
                Offer offer = offers[i];
                if (offer.SentAt > 0f) continue;

                StackableItemAsset stack = offer.Stack;
                if (stack != null)
                {
                    offer.Expected = bag.Count(stack) - offer.Count;
                    exchanger.GiveStackableRpc(takerId, stack.NetworkId, offer.Count);
                }
                else
                {
                    exchanger.GiveUniqueRpc(takerId, offer.Slot);
                }

                offer.SentAt = Time.unscaledTime;
                offers[i] = offer;
                sent++;
            }

            stale = true;
            Log.Info($"Handed {sent} things over to {taker.name}");
        }

        private void Look()
        {
            Character seen = Taker(taker);
            if (ReferenceEquals(seen, taker)) return;

            Log.Info(seen == null
                ? "Nobody in view can take things now, the offer returns to the bag"
                : $"The bag offers things to {seen.name} now");

            taker = seen;
            offers.RemoveAll(offer => offer.SentAt <= 0f);
            stale = true;
        }

        // The living character nearest to the middle of the view; the one already chosen keeps a margin,
        // so the panel does not blink at the edge of the cone or the radius
        private Character Taker(Character current)
        {
            Camera view = Camera.main;
            if (view == null || own == null || bag == null) return null;

            Transform eyes = view.transform;
            if (current != null && Offside(current, eyes, Stickiness) <= 1f) return current;

            Character best = null;
            float bestScore = float.PositiveInfinity;

            Character.ForEach(candidate =>
            {
                float score = Offside(candidate, eyes, 1f);
                if (score > 1f || score >= bestScore) return;

                best = candidate;
                bestScore = score;
            }, RegisterInactivePolicy.Exclude);

            return best;
        }

        // A share of the cone, above 1 is out of reach; at arm's length a body covers more than the cone
        private float Offside(Character candidate, Transform eyes, float slack)
        {
            if (candidate == null || candidate == own || !candidate.gameObject.activeInHierarchy) return float.PositiveInfinity;
            if (candidate.GetComponentInChildren<InventoryExchanger>() == null) return float.PositiveInfinity;

            Health health = candidate.GetComponent<Health>();
            if (health != null && !health.Alive) return float.PositiveInfinity;

            if (Vector3.Distance(own.transform.position, candidate.transform.position) > exchanger.Radius * slack)
                return float.PositiveInfinity;

            Vector3 toward = candidate.transform.position - eyes.position;
            float distance = toward.magnitude;
            if (distance < 0.01f) return float.PositiveInfinity;

            float off = float.PositiveInfinity;
            foreach (float height in BodyHeights)
                off = Mathf.Min(off, Vector3.Angle(eyes.forward, toward + Vector3.up * height));

            float allowed = Mathf.Max(ConeAngle, Mathf.Atan2(BodyRadius, distance) * Mathf.Rad2Deg) * slack;
            float score = off / allowed;

            return score <= 1f && Visible(eyes, candidate, toward / distance, distance) ? score : float.PositiveInfinity;
        }

        private bool Visible(Transform eyes, Character candidate, Vector3 direction, float distance)
        {
            int found = Interactor.Look(eyes.position, direction, distance, eyes.root, sights);
            Transform body = candidate.transform.root;

            for (int i = 0; i < found; i++)
                if (!sights[i].transform.IsChildOf(body) && sights[i].distance < distance - BodyRadius)
                    return false;

            return true;
        }

        private static VisualElement Standing(ItemAsset asset, int row, int column)
        {
            VisualElement thing = Slot(asset, asset.Id, false, true);
            thing.style.position = Position.Absolute;
            thing.style.left = column * Cell;
            thing.style.top = row * Cell;
            thing.style.width = Cell;
            thing.style.height = Cell;

            return thing;
        }

        private static void Amount(VisualElement thing, int count)
        {
            var label = new Label(count.ToString());
            label.AddToClassList("slot__amount");
            thing.Add(label);
        }

        // The bounding box of the filled cells against the recipe's, so a one-cell recipe matches in any cell
        private Craft Match()
        {
            if (crafter == null || bag == null) return null;

            var standing = new StackableItemAsset[bench.Length];
            for (int i = 0; i < bench.Length; i++) standing[i] = bench[i].Asset;

            if (!Shape(standing, out StackableItemAsset[] placed, out int width, out int height)) return null;

            foreach (Craft craft in crafter.AvailableCrafts)
            {
                if (craft == null || craft.Output == null || craft.Input == null || craft.Input.Length != bench.Length) continue;
                if (!Shape(craft.Input, out StackableItemAsset[] recipe, out int recipeWidth, out int recipeHeight)) continue;
                if (recipeWidth != width || recipeHeight != height) continue;

                bool same = true;
                for (int i = 0; i < placed.Length && same; i++) same = placed[i] == recipe[i];
                if (same) return craft;
            }

            return null;
        }

        private static bool Shape(StackableItemAsset[] cells, out StackableItemAsset[] shape, out int width, out int height)
        {
            int top = CraftSide, left = CraftSide, bottom = -1, right = -1;
            for (int i = 0; i < cells.Length; i++)
            {
                if (cells[i] == null) continue;

                int row = i / CraftSide;
                int column = i % CraftSide;
                top = Math.Min(top, row);
                bottom = Math.Max(bottom, row);
                left = Math.Min(left, column);
                right = Math.Max(right, column);
            }

            shape = null;
            width = 0;
            height = 0;
            if (bottom < 0) return false;

            width = right - left + 1;
            height = bottom - top + 1;
            shape = new StackableItemAsset[width * height];
            for (int row = 0; row < height; row++)
            for (int column = 0; column < width; column++)
                shape[row * width + column] = cells[(top + row) * CraftSide + left + column];

            return true;
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

        private static bool Pack(bool[,] taken, ItemAsset asset, int rows, int columns, out int row, out int column)
        {
            Vector2Int size = asset == null ? Vector2Int.one : asset.Cells;

            for (row = 0; row + size.y <= rows; row++)
            for (column = 0; column + size.x <= columns; column++)
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

        private VisualElement Thing(ItemAsset asset, string fallback, int row, int column, string amount, int slot,
            bool equipable, bool holding, StackableItemAsset stack = null, int split = -1)
        {
            Vector2Int cells = asset == null ? Vector2Int.one : asset.Cells;
            var size = new Vector2(cells.x * Cell, cells.y * Cell);
            VisualElement thing = Slot(asset, fallback, holding, equipable);

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

            if (stack != null)
                Draggable(thing, Icon(asset), size, () =>
                {
                    draggedStack = stack;
                    draggedSplit = split;
                });
            else
                Draggable(thing, Icon(asset), size, () =>
                {
                    dragged = slot;
                    draggedUnique = true;
                    draggedEquipable = equipable;
                    draggedFromHands = holding;
                });

            return thing;
        }

        private static Sprite Icon(ItemAsset asset)
        {
            return asset == null || asset.Icon == null ? null : asset.Icon.Sprite;
        }

        private void Draggable(VisualElement thing, Sprite icon, Vector2 size, Action begin)
        {
            thing.RegisterCallback<PointerDownEvent>(down =>
            {
                if (down.button != 0 || ghost != null) return;

                begin();
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

            if (bag != null) Dropped(at);

            draggedStack = null;
            draggedSplit = -1;
            draggedCell = -1;
            draggedOffer = -1;
            draggedOutput = false;
            draggedUnique = false;
            draggedEquipable = false;
        }

        private void Dropped(Vector2 at)
        {
            bool overBag = grid.worldBound.Contains(at) || held.worldBound.Contains(at);
            bool overGive = taker != null && give.worldBound.Contains(at);
            int cell = BenchCellAt(at);

            if (draggedOutput)
            {
                Craft match = Match();
                if (match == null || !overBag) return;

                // The bench pays when the bag confirms the result arrived
                pendingCraft = match;
                outputBefore = Counted(match.Output);
                crafter.CraftRpc(match.NetworkId);
                return;
            }

            if (draggedOffer >= 0)
            {
                if (!overGive && draggedOffer < offers.Count) offers.RemoveAt(draggedOffer);

                stale = true;
                return;
            }

            if (draggedCell >= 0)
            {
                Unit moved = bench[draggedCell];

                if (overGive)
                {
                    if (moved.Asset != null && Propose(moved.Asset, moved.Count)) bench[draggedCell] = default;
                }
                else if (cell < 0) bench[draggedCell] = default;
                else if (cell != draggedCell && bench[cell].Asset == null)
                {
                    bench[cell] = moved;
                    bench[draggedCell] = default;
                }
                else if (cell != draggedCell && bench[cell].Asset == moved.Asset)
                {
                    bench[cell].Count += moved.Count;
                    bench[draggedCell] = default;
                }

                stale = true;
                return;
            }

            if (draggedStack != null)
            {
                bool splitOff = draggedSplit >= 0 && draggedSplit < splits.Count;
                int amount = splitOff ? splits[draggedSplit].Count : bag.Count(draggedStack) - Reserved(draggedStack);
                if (amount <= 0) return;

                bool moved = overGive ? Propose(draggedStack, amount) : cell >= 0 && Lay(cell, draggedStack, amount);
                if (!moved) return;

                if (splitOff) splits.RemoveAt(draggedSplit);
                stale = true;
                return;
            }

            if (!draggedUnique) return;

            if (overGive)
            {
                if (Propose(dragged)) stale = true;
                return;
            }

            if (!draggedEquipable) return;

            if (held.worldBound.Contains(at) && !draggedFromHands) holder.HoldRpc(dragged);
            else if (grid.worldBound.Contains(at) && draggedFromHands) holder.HoldRpc(Inventory.NoSlot);
        }

        private bool Lay(int cell, StackableItemAsset asset, int amount)
        {
            if (bench[cell].Asset == null) bench[cell] = new Unit { Asset = asset, Count = amount };
            else if (bench[cell].Asset == asset) bench[cell].Count += amount;
            else return false;

            return true;
        }

        private void AddMenu(VisualElement thing, StackableItemAsset stack, int amount, int slot)
        {
            thing.RegisterCallback<PointerDownEvent>(down =>
            {
                if (down.button != 1 || ghost != null) return;

                OpenMenu(down.position, stack, amount, slot);
                down.StopPropagation();
            });
        }

        private void OpenMenu(Vector2 at, StackableItemAsset stack, int amount, int slot)
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

            if (stack != null && stack.Usable) Item(menu, "Использовать", () => user.UseRpc(stack.NetworkId));
            if (stack != null && amount > 1) Item(menu, "Разделить", () => AskSplit(stack));

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

        private void CloseMenu()
        {
            if (curtain == null) return;

            curtain.RemoveFromHierarchy();
            curtain = null;
        }

        // While the number is typed the player's keys belong to the field: Escape closes only this box
        private void AskSplit(StackableItemAsset asset)
        {
            LocalPlayer player = OwnPlayer.Find<LocalPlayer>();
            if (player == null) return;

            HidePrompt();
            player.OpenPrompt();

            prompt = new VisualElement();
            prompt.AddToClassList("menu-curtain");
            prompt.AddToClassList("split-curtain");
            prompt.RegisterCallback<PointerDownEvent>(down =>
            {
                if (down.target == prompt) EndPrompt();
            });

            var box = new VisualElement();
            box.AddToClassList("split");

            var head = new VisualElement();
            head.AddToClassList("split__head");

            var close = new Button(EndPrompt) { text = "×" };
            close.AddToClassList("split__close");
            head.Add(close);

            var title = new Label($"Разделить: {asset.Title}");
            title.AddToClassList("line");
            title.AddToClassList("split__title");
            head.Add(title);
            box.Add(head);

            int free = bag.Count(asset) - Reserved(asset);
            var field = new TextField { maxLength = SplitDigits, value = Math.Max(1, free / 2).ToString() };
            field.AddToClassList("split__field");
            field.RegisterValueChangedCallback(changed =>
            {
                string digits = Digits(changed.newValue);
                if (digits != changed.newValue) field.SetValueWithoutNotify(digits);
            });
            field.RegisterCallback<KeyDownEvent>(typed =>
            {
                if (typed.keyCode != KeyCode.Return && typed.keyCode != KeyCode.KeypadEnter) return;

                typed.StopPropagation();
                Split(asset, field.value);
            });
            box.Add(field);

            var confirm = new Button(() => Split(asset, field.value)) { text = "Разделить" };
            confirm.AddToClassList("split__button");
            box.Add(confirm);

            prompt.Add(box);
            window.Add(prompt);
            field.schedule.Execute(() =>
            {
                field.Focus();
                field.SelectAll();
            });
        }

        private void Split(StackableItemAsset asset, string typed)
        {
            int free = bag == null ? 0 : bag.Count(asset) - Reserved(asset);
            if (!int.TryParse(typed, out int count) || count <= 0 || count >= free) return;

            splits.Add(new Unit { Asset = asset, Count = count });
            EndPrompt();
            stale = true;
            Log.Info($"Split {count} of {free} {asset.Id} off in the bag");
        }

        private static string Digits(string typed)
        {
            if (string.IsNullOrEmpty(typed)) return string.Empty;

            var digits = new System.Text.StringBuilder(typed.Length);
            foreach (char c in typed)
                if (c >= '0' && c <= '9')
                    digits.Append(c);

            return digits.ToString();
        }

        private void EndPrompt()
        {
            if (prompt == null) return;

            OwnPlayer.Find<LocalPlayer>()?.ClosePrompt();
            HidePrompt();
        }

        private void HidePrompt()
        {
            if (prompt == null) return;

            prompt.RemoveFromHierarchy();
            prompt = null;
        }

        private static VisualElement Slot(ItemAsset asset, string fallback, bool holding, bool equipable)
        {
            Vector2Int size = asset == null ? Vector2Int.one : asset.Cells;

            var slot = new VisualElement { tooltip = asset == null ? fallback : asset.Title };
            slot.AddToClassList("slot");
            if (holding) slot.AddToClassList("slot--held");
            if (!equipable) slot.AddToClassList("slot--fixed");

            if (Icon(asset) != null)
            {
                var icon = new VisualElement();
                icon.AddToClassList("slot__icon");
                icon.style.backgroundImage = Background.FromSprite(Icon(asset));
                icon.style.width = size.x * Cell - Bezel;
                icon.style.height = size.y * Cell - Bezel;
                slot.Add(icon);
            }
            else
            {
                var name = new Label(asset == null ? fallback : asset.Title);
                name.AddToClassList("slot__name");
                slot.Add(name);
            }

            return slot;
        }
    }
}
