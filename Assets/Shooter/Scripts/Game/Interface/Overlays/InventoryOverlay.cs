using System;
using System.Collections.Generic;
using System.Text;
using Shooter.Infrastructure;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Game
{
    public class InventoryOverlay : Overlay
    {
        private const string WindowElementName = "inventory-screen";
        private const string InventoryGridElementName = "inventory-grid";
        private const string HandsGridElementName = "inventory-held";
        private const string CraftGridElementName = "craft-grid";
        private const string CraftOutputElementName = "craft-output";
        private const string GivePanelElementName = "give";
        private const string GiveTitleLabelName = "give-title";
        private const string GiveGridElementName = "give-grid";
        private const string GiveButtonName = "give-button";
        private const int CraftGridSide = 3;
        private const int GiveGridColumns = 4;
        private const int GiveGridRows = 3;
        private const float CellSize = 48f;
        private const float IconBezel = 8f;
        private const int InventoryGridColumns = 10;
        private const int InventoryGridRows = 6;
        private const int HandsGridRows = 2;
        private const float TakerConeAngle = 12f;
        private const float BodyRadius = 0.6f;
        private const float SentOfferTimeoutSeconds = 3f;
        private const float TakerSearchIntervalSeconds = 0.2f;
        private const float ChosenTakerSlack = 1.25f;
        private const int SplitAmountMaxDigits = 7;
        private static readonly Journal Log = Logs.CreateJournalForThisFile();

        // Points along a body from feet to head around its root, which sits at the middle of the body for players and residents alike
        private static readonly float[] BodyHeights = { -0.8f, -0.4f, 0f, 0.4f, 0.8f };

        private struct ItemStack
        {
            public StackableItemAsset Asset;
            public int Count;
        }

        private struct OfferedItem
        {
            public ItemAsset Asset;
            public string AssetId;
            public int Count;
            public int Slot;
            public int ExpectedCountAfterGive;
            public float SentTime;

            public StackableItemAsset StackableAsset => Slot == Inventory.NoSlot ? Asset as StackableItemAsset : null;
        }

        private readonly ItemStack[] craftGridStacks = new ItemStack[CraftGridSide * CraftGridSide];
        private readonly List<ItemStack> splitStacks = new();
        private readonly List<OfferedItem> offeredItems = new();
        private readonly NameMapper nameMapper = new();
        private readonly RaycastHit[] lineOfSightHits = new RaycastHit[16];
        private Inventory inventory;
        private InventoryHolder inventoryHolder;
        private InventoryExchanger inventoryExchanger;
        private InventoryUser inventoryUser;
        private Crafter crafter;
        private Character ownCharacter;
        private Character chosenTaker;
        private float nextTakerSearchTime;
        private VisualElement craftGridElement;
        private VisualElement craftOutputElement;
        private VisualElement givePanelElement;
        private Label giveTitleLabel;
        private VisualElement giveGridElement;
        private Button giveButton;
        private StackableItemAsset draggedStackableAsset;
        private int draggedSplitIndex = -1;
        private int draggedCraftGridCell = -1;
        private int draggedOfferIndex = -1;
        private bool craftOutputDragged;
        private bool uniqueItemDragged;
        private bool draggedItemEquipable;
        private CraftAsset pendingCraft;
        private int outputCountBeforeCraft;
        private VisualElement contextMenuCurtainElement;
        private VisualElement splitPromptElement;
        private int draggedUniqueSlot;
        private bool draggedFromHands;
        private VisualElement dragGhostElement;
        private VisualElement inventoryGridElement;
        private VisualElement handsGridElement;
        private bool open;
        private int dragPointerId;
        private bool stale;

        private VisualElement windowElement;

        private void Update()
        {
            if (!IsBound) return;

            LocalPlayer localPlayer = OwnPlayer.FindComponent<LocalPlayer>();
            bool shouldBeOpen = localPlayer != null && localPlayer.IsInventoryOpen;

            if (shouldBeOpen != open)
            {
                open = shouldBeOpen;

                if (open)
                {
                    OpenWindow();
                }
                else
                {
                    CloseWindow();
                }
            }

            if (!open) return;

            if (splitPromptElement != null && (localPlayer == null || !localPlayer.IsPromptOpen)) HideSplitPrompt();

            UpdateTakerAndOffers();
            if (stale) RedrawInventory();
        }

        protected override bool TryBindElements(VisualElement root)
        {
            windowElement = root.Q<VisualElement>(WindowElementName);
            inventoryGridElement = root.Q<VisualElement>(InventoryGridElementName);
            handsGridElement = root.Q<VisualElement>(HandsGridElementName);
            craftGridElement = root.Q<VisualElement>(CraftGridElementName);
            craftOutputElement = root.Q<VisualElement>(CraftOutputElementName);
            givePanelElement = root.Q<VisualElement>(GivePanelElementName);
            giveTitleLabel = root.Q<Label>(GiveTitleLabelName);
            giveGridElement = root.Q<VisualElement>(GiveGridElementName);
            giveButton = root.Q<Button>(GiveButtonName);

            if (windowElement == null || inventoryGridElement == null || handsGridElement == null || craftGridElement == null || craftOutputElement == null || givePanelElement == null || giveTitleLabel == null || giveGridElement == null || giveButton == null)
            {
                Log.Error($"Overlay document has no {WindowElementName} window, the bag stays hidden");
                return false;
            }

            windowElement.style.display = DisplayStyle.None;
            givePanelElement.style.display = DisplayStyle.None;
            giveButton.clicked += GiveOfferedItems;

            return true;
        }

        protected override void UnbindElements()
        {
            if (open) CloseWindow();

            if (giveButton != null) giveButton.clicked -= GiveOfferedItems;

            open = false;
            windowElement = null;
        }

        private void OpenWindow()
        {
            inventory = OwnPlayer.FindComponent<Inventory>();
            inventoryHolder = OwnPlayer.FindComponent<InventoryHolder>();
            inventoryExchanger = OwnPlayer.FindComponent<InventoryExchanger>();
            inventoryUser = OwnPlayer.FindComponent<InventoryUser>();
            crafter = OwnPlayer.FindComponent<Crafter>();
            ownCharacter = OwnPlayer.FindComponent<Character>();
            chosenTaker = FindTakerNearestToViewCenter(null);
            nextTakerSearchTime = Time.unscaledTime + TakerSearchIntervalSeconds;

            if (inventory != null) inventory.Changed += MarkStale;
            if (inventoryHolder != null) inventoryHolder.Changed += MarkStale;

            windowElement.style.display = DisplayStyle.Flex;
            stale = true;
            Log.Info(chosenTaker == null ? "The bag is open" : $"The bag is open, {chosenTaker.name} can take things from it");
        }

        private void CloseWindow()
        {
            CloseContextMenu();
            CloseSplitPrompt();

            if (inventory != null) inventory.Changed -= MarkStale;
            if (inventoryHolder != null) inventoryHolder.Changed -= MarkStale;
            inventory = null;
            inventoryHolder = null;
            inventoryExchanger = null;
            inventoryUser = null;
            crafter = null;
            ownCharacter = null;
            chosenTaker = null;
            pendingCraft = null;
            Array.Clear(craftGridStacks, 0, craftGridStacks.Length);
            splitStacks.Clear();
            offeredItems.Clear();

            if (windowElement != null) windowElement.style.display = DisplayStyle.None;
            if (givePanelElement != null) givePanelElement.style.display = DisplayStyle.None;
            inventoryGridElement?.Clear();
            handsGridElement?.Clear();
            craftGridElement?.Clear();
            craftOutputElement?.Clear();
            giveGridElement?.Clear();
            Log.Info("The bag is closed");
        }

        private void MarkStale()
        {
            stale = true;
        }

        private void UpdateTakerAndOffers()
        {
            if (Time.unscaledTime >= nextTakerSearchTime)
            {
                nextTakerSearchTime = Time.unscaledTime + TakerSearchIntervalSeconds;
                UpdateChosenTaker();
            }

            if (chosenTaker == null) return;

            bool hasUnsentOffer = false;
            for (int i = 0; i < offeredItems.Count; i++)
            {
                OfferedItem offeredItem = offeredItems[i];
                if (offeredItem.SentTime <= 0f)
                {
                    hasUnsentOffer = true;
                    continue;
                }

                if (Time.unscaledTime - offeredItem.SentTime <= SentOfferTimeoutSeconds) continue;

                Log.Info($"The bag never let {offeredItem.AssetId} x {offeredItem.Count} go, it is back in the offer");
                offeredItem.SentTime = 0f;
                offeredItems[i] = offeredItem;
                stale = true;
            }

            bool isTakerNear = ownCharacter != null && inventoryExchanger != null && Vector3.Distance(ownCharacter.transform.position, chosenTaker.transform.position) <= inventoryExchanger.Radius;
            giveButton.SetEnabled(isTakerNear && hasUnsentOffer);
        }

        private void RedrawInventory()
        {
            stale = false;
            CloseContextMenu();
            ReconcileWithInventory();
            inventoryGridElement.Clear();
            handsGridElement.Clear();

            DrawGridCells(handsGridElement, HandsGridRows);
            DrawGridCells(inventoryGridElement, InventoryGridRows);

            if (inventory == null) return;

            ItemCatalog itemCatalog = Catalogs.GetCatalog<ItemCatalog>();
            UniqueItem heldItem = inventoryHolder.HeldItem;
            int heldSlot = inventoryHolder.HeldSlot;
            bool[,] takenCells = new bool[InventoryGridRows, InventoryGridColumns];

            if (heldItem != null && !IsSlotOffered(heldSlot))
            {
                ItemAsset asset = itemCatalog == null ? null : itemCatalog.GetAssetById(heldItem.AssetId);

                VisualElement itemElement = CreateItemElement(asset, heldItem.AssetId, 0, 0, null, heldSlot, true, true);
                AddContextMenu(itemElement, null, 0, heldSlot);
                handsGridElement.Add(itemElement);
            }

            IReadOnlyList<UniqueItem> uniqueItems = inventory.UniqueItems;

            for (int slot = 0; slot < uniqueItems.Count; slot++)
            {
                UniqueItem uniqueItem = uniqueItems[slot];
                if (uniqueItem == null || slot == heldSlot || IsSlotOffered(slot)) continue;

                ItemAsset asset = itemCatalog == null ? null : itemCatalog.GetAssetById(uniqueItem.AssetId);
                TryPlaceInGrid(takenCells, asset, InventoryGridRows, InventoryGridColumns, out int row, out int column);

                VisualElement itemElement = CreateItemElement(asset, uniqueItem.AssetId, row, column, null, slot, asset is UniqueItemAsset uniqueItemAsset && uniqueItemAsset.IsEquipable, false);
                AddContextMenu(itemElement, null, 0, slot);
                inventoryGridElement.Add(itemElement);
            }

            int assetCount = itemCatalog == null ? 0 : itemCatalog.Count;

            for (int index = 0; index < assetCount; index++)
            {
                if (itemCatalog.GetAssetAt(index) is not StackableItemAsset asset) continue;

                int amount = inventory.CountStackable(asset) - CountReserved(asset);
                if (amount > 0)
                {
                    TryPlaceInGrid(takenCells, asset, InventoryGridRows, InventoryGridColumns, out int row, out int column);

                    VisualElement itemElement = CreateItemElement(asset, asset.Id, row, column, amount.ToString(), Inventory.NoSlot, false, false, asset);
                    AddContextMenu(itemElement, asset, amount, Inventory.NoSlot);
                    inventoryGridElement.Add(itemElement);
                }

                for (int i = 0; i < splitStacks.Count; i++)
                {
                    if (splitStacks[i].Asset != asset) continue;

                    TryPlaceInGrid(takenCells, asset, InventoryGridRows, InventoryGridColumns, out int row, out int column);
                    inventoryGridElement.Add(CreateItemElement(asset, asset.Id, row, column, splitStacks[i].Count.ToString(), Inventory.NoSlot, false, false, asset, i));
                }
            }

            DrawCraftGrid();
            DrawGivePanel();
        }

        private int CountOnCraftGrid(StackableItemAsset asset)
        {
            int count = 0;
            foreach (ItemStack craftGridStack in craftGridStacks)
            {
                if (craftGridStack.Asset == asset) count += craftGridStack.Count;
            }

            return count;
        }

        private int CountReserved(StackableItemAsset asset)
        {
            int reservedCount = CountOnCraftGrid(asset);

            foreach (ItemStack splitStack in splitStacks)
            {
                if (splitStack.Asset == asset) reservedCount += splitStack.Count;
            }

            foreach (OfferedItem offeredItem in offeredItems)
            {
                if (offeredItem.StackableAsset == asset) reservedCount += offeredItem.Count;
            }

            return reservedCount;
        }

        private bool IsSlotOffered(int slot)
        {
            foreach (OfferedItem offeredItem in offeredItems)
            {
                if (offeredItem.Slot != Inventory.NoSlot && offeredItem.Slot == slot) return true;
            }

            return false;
        }

        // A result that reached the inventory pays one unit from every cell; an offer the inventory let go is done;
        // what the inventory no longer backs leaves the splits first, then the offer, then the craft grid
        private void ReconcileWithInventory()
        {
            if (inventory == null)
            {
                Array.Clear(craftGridStacks, 0, craftGridStacks.Length);
                splitStacks.Clear();
                offeredItems.Clear();
                return;
            }

            if (pendingCraft != null && CountInInventory(pendingCraft.Output) > outputCountBeforeCraft)
            {
                for (int i = 0; i < craftGridStacks.Length; i++)
                {
                    if (craftGridStacks[i].Asset != null) TakeFromCraftGridCell(i, 1);
                }

                pendingCraft = null;
            }

            IReadOnlyList<UniqueItem> uniqueItems = inventory.UniqueItems;

            for (int i = offeredItems.Count - 1; i >= 0; i--)
            {
                OfferedItem offeredItem = offeredItems[i];
                StackableItemAsset stackableAsset = offeredItem.StackableAsset;

                bool isGiven = stackableAsset == null
                    ? offeredItem.Slot >= uniqueItems.Count || uniqueItems[offeredItem.Slot] == null || uniqueItems[offeredItem.Slot].AssetId != offeredItem.AssetId
                    : offeredItem.SentTime > 0f && inventory.CountStackable(stackableAsset) <= offeredItem.ExpectedCountAfterGive;

                if (isGiven) offeredItems.RemoveAt(i);
            }

            for (int i = splitStacks.Count - 1; i >= 0; i--)
            {
                ItemStack splitStack = splitStacks[i];
                int excess = CountReserved(splitStack.Asset) - inventory.CountStackable(splitStack.Asset);
                if (excess <= 0) continue;

                splitStack.Count -= Math.Min(excess, splitStack.Count);
                if (splitStack.Count <= 0)
                {
                    splitStacks.RemoveAt(i);
                }
                else
                {
                    splitStacks[i] = splitStack;
                }
            }

            for (int i = offeredItems.Count - 1; i >= 0; i--)
            {
                OfferedItem offeredItem = offeredItems[i];
                StackableItemAsset stackableAsset = offeredItem.StackableAsset;
                if (stackableAsset == null || offeredItem.SentTime > 0f) continue;

                int excess = CountReserved(stackableAsset) - inventory.CountStackable(stackableAsset);
                if (excess <= 0) continue;

                offeredItem.Count -= Math.Min(excess, offeredItem.Count);
                if (offeredItem.Count <= 0)
                {
                    offeredItems.RemoveAt(i);
                }
                else
                {
                    offeredItems[i] = offeredItem;
                }
            }

            for (int i = craftGridStacks.Length - 1; i >= 0; i--)
            {
                StackableItemAsset asset = craftGridStacks[i].Asset;
                if (asset == null) continue;

                int excess = CountReserved(asset) - inventory.CountStackable(asset);
                if (excess > 0) TakeFromCraftGridCell(i, Math.Min(excess, craftGridStacks[i].Count));
            }
        }

        private void TakeFromCraftGridCell(int cell, int count)
        {
            craftGridStacks[cell].Count -= count;
            if (craftGridStacks[cell].Count <= 0) craftGridStacks[cell] = default;
        }

        private int CountInInventory(ItemAsset asset)
        {
            if (inventory == null) return 0;
            if (asset is StackableItemAsset stackableAsset) return inventory.CountStackable(stackableAsset);

            int uniqueItemCount = 0;
            foreach (UniqueItem uniqueItem in inventory.UniqueItems)
            {
                if (uniqueItem != null) uniqueItemCount++;
            }

            return uniqueItemCount;
        }

        private void DrawCraftGrid()
        {
            craftGridElement.Clear();
            craftOutputElement.Clear();

            DrawGridCells(craftGridElement, CraftGridSide, CraftGridSide);
            DrawGridCells(craftOutputElement, 1, 1);

            for (int i = 0; i < craftGridStacks.Length; i++)
            {
                ItemStack craftGridStack = craftGridStacks[i];
                if (craftGridStack.Asset == null) continue;

                int cell = i;
                VisualElement itemElement = CreateOneCellItemElement(craftGridStack.Asset, i / CraftGridSide, i % CraftGridSide);
                if (craftGridStack.Count > 1) AddAmountLabel(itemElement, craftGridStack.Count);

                MakeDraggable(itemElement, GetIconSprite(craftGridStack.Asset), new Vector2(CellSize, CellSize), () => draggedCraftGridCell = cell);
                itemElement.RegisterCallback<PointerDownEvent>(pointerDown =>
                {
                    if (pointerDown.button != 1 || dragGhostElement != null) return;

                    craftGridStacks[cell] = default;
                    stale = true;
                    pointerDown.StopPropagation();
                });
                craftGridElement.Add(itemElement);
            }

            CraftAsset matchingCraft = FindMatchingCraft();
            craftOutputElement.EnableInClassList("craft__output--ready", matchingCraft != null);
            if (matchingCraft == null) return;

            VisualElement outputElement = CreateOneCellItemElement(matchingCraft.Output, 0, 0);
            MakeDraggable(outputElement, GetIconSprite(matchingCraft.Output), new Vector2(CellSize, CellSize), () => craftOutputDragged = true);
            craftOutputElement.Add(outputElement);
        }

        private void DrawGivePanel()
        {
            giveGridElement.Clear();
            givePanelElement.style.display = chosenTaker == null ? DisplayStyle.None : DisplayStyle.Flex;
            if (chosenTaker == null) return;

            string takerName = nameMapper.GetShownNameById(chosenTaker.Id);
            giveTitleLabel.text = string.IsNullOrEmpty(takerName) ? "Передать" : $"Передать: {takerName}";
            DrawGridCells(giveGridElement, GiveGridRows, GiveGridColumns);

            bool[,] takenCells = new bool[GiveGridRows, GiveGridColumns];

            for (int i = 0; i < offeredItems.Count; i++)
            {
                OfferedItem offeredItem = offeredItems[i];
                TryPlaceInGrid(takenCells, offeredItem.Asset, GiveGridRows, GiveGridColumns, out int row, out int column);

                Vector2Int sizeInCells = offeredItem.Asset == null ? Vector2Int.one : offeredItem.Asset.SizeInCells;
                Vector2 size = new(sizeInCells.x * CellSize, sizeInCells.y * CellSize);
                VisualElement itemElement = CreateSlotElement(offeredItem.Asset, offeredItem.AssetId, false, offeredItem.SentTime <= 0f);
                itemElement.style.position = Position.Absolute;
                itemElement.style.left = column * CellSize;
                itemElement.style.top = row * CellSize;
                itemElement.style.width = size.x;
                itemElement.style.height = size.y;

                if (offeredItem.StackableAsset != null) AddAmountLabel(itemElement, offeredItem.Count);

                if (offeredItem.SentTime > 0f)
                {
                    itemElement.AddToClassList("slot--sent");
                    giveGridElement.Add(itemElement);
                    continue;
                }

                int offerIndex = i;
                MakeDraggable(itemElement, GetIconSprite(offeredItem.Asset), size, () => draggedOfferIndex = offerIndex);
                itemElement.RegisterCallback<PointerDownEvent>(pointerDown =>
                {
                    if (pointerDown.button != 1 || dragGhostElement != null) return;

                    if (offerIndex < offeredItems.Count) offeredItems.RemoveAt(offerIndex);
                    stale = true;
                    pointerDown.StopPropagation();
                });
                giveGridElement.Add(itemElement);
            }
        }

        private bool TryOfferStackable(StackableItemAsset asset, int amount)
        {
            for (int i = 0; i < offeredItems.Count; i++)
            {
                OfferedItem offeredItem = offeredItems[i];
                if (offeredItem.StackableAsset != asset || offeredItem.SentTime > 0f) continue;

                offeredItem.Count += amount;
                offeredItems[i] = offeredItem;
                return true;
            }

            if (!HasGiveGridRoomFor(asset)) return false;

            offeredItems.Add(new OfferedItem { Asset = asset, AssetId = asset.Id, Count = amount, Slot = Inventory.NoSlot });
            return true;
        }

        private bool TryOfferUnique(int slot)
        {
            if (inventory == null || IsSlotOffered(slot)) return false;

            IReadOnlyList<UniqueItem> uniqueItems = inventory.UniqueItems;
            UniqueItem uniqueItem = slot >= 0 && slot < uniqueItems.Count ? uniqueItems[slot] : null;
            if (uniqueItem == null) return false;

            ItemCatalog itemCatalog = Catalogs.GetCatalog<ItemCatalog>();
            ItemAsset asset = itemCatalog == null ? null : itemCatalog.GetAssetById(uniqueItem.AssetId);
            if (!HasGiveGridRoomFor(asset)) return false;

            offeredItems.Add(new OfferedItem { Asset = asset, AssetId = uniqueItem.AssetId, Count = 1, Slot = slot });
            return true;
        }

        private bool HasGiveGridRoomFor(ItemAsset extraAsset)
        {
            bool[,] takenCells = new bool[GiveGridRows, GiveGridColumns];

            foreach (OfferedItem offeredItem in offeredItems)
            {
                if (!TryPlaceInGrid(takenCells, offeredItem.Asset, GiveGridRows, GiveGridColumns, out _, out _)) return false;
            }

            return TryPlaceInGrid(takenCells, extraAsset, GiveGridRows, GiveGridColumns, out _, out _);
        }

        // The inventory only lets go once the server confirms; until then the offer stays greyed out where it was
        private void GiveOfferedItems()
        {
            if (inventory == null || chosenTaker == null) return;

            long takerId = chosenTaker.Id;
            int sentCount = 0;

            for (int i = 0; i < offeredItems.Count; i++)
            {
                OfferedItem offeredItem = offeredItems[i];
                if (offeredItem.SentTime > 0f) continue;

                StackableItemAsset stackableAsset = offeredItem.StackableAsset;
                if (stackableAsset != null)
                {
                    offeredItem.ExpectedCountAfterGive = inventory.CountStackable(stackableAsset) - offeredItem.Count;
                    inventoryExchanger.GiveStackableRpc(takerId, stackableAsset.NetworkId, offeredItem.Count);
                }
                else
                {
                    inventoryExchanger.GiveUniqueRpc(takerId, offeredItem.Slot);
                }

                offeredItem.SentTime = Time.unscaledTime;
                offeredItems[i] = offeredItem;
                sentCount++;
            }

            stale = true;
            Log.Info($"Handed {sentCount} things over to {chosenTaker.name}");
        }

        private void UpdateChosenTaker()
        {
            Character foundTaker = FindTakerNearestToViewCenter(chosenTaker);
            if (ReferenceEquals(foundTaker, chosenTaker)) return;

            Log.Info(foundTaker == null
                ? "Nobody in view can take things now, the offer returns to the bag"
                : $"The bag offers things to {foundTaker.name} now");

            chosenTaker = foundTaker;
            offeredItems.RemoveAll(offeredItem => offeredItem.SentTime <= 0f);
            stale = true;
        }

        // The one already chosen keeps a margin, so the panel does not blink at the edge of the cone or the radius
        private Character FindTakerNearestToViewCenter(Character currentTaker)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null || ownCharacter == null || inventory == null) return null;

            Transform eyes = mainCamera.transform;
            if (currentTaker != null && ScoreTakerCandidate(currentTaker, eyes, ChosenTakerSlack) <= 1f) return currentTaker;

            Character bestCandidate = null;
            float bestScore = float.PositiveInfinity;

            Character.ForEach(candidate =>
            {
                float score = ScoreTakerCandidate(candidate, eyes, 1f);
                if (score > 1f || score >= bestScore) return;

                bestCandidate = candidate;
                bestScore = score;
            }, RegisterInactivePolicy.Exclude);

            return bestCandidate;
        }

        // A share of the cone, above 1 is out of reach; at arm's length a body covers more than the cone
        private float ScoreTakerCandidate(Character candidate, Transform eyes, float slack)
        {
            if (candidate == null || candidate == ownCharacter || !candidate.gameObject.activeInHierarchy) return float.PositiveInfinity;
            if (candidate.GetComponentInChildren<InventoryExchanger>() == null) return float.PositiveInfinity;

            Health health = candidate.GetComponent<Health>();
            if (health != null && !health.IsAlive) return float.PositiveInfinity;

            if (Vector3.Distance(ownCharacter.transform.position, candidate.transform.position) > inventoryExchanger.Radius * slack) return float.PositiveInfinity;

            Vector3 towardCandidate = candidate.transform.position - eyes.position;
            float distance = towardCandidate.magnitude;
            if (distance < 0.01f) return float.PositiveInfinity;

            float smallestAngle = float.PositiveInfinity;
            foreach (float height in BodyHeights)
            {
                smallestAngle = Mathf.Min(smallestAngle, Vector3.Angle(eyes.forward, towardCandidate + Vector3.up * height));
            }

            float allowedAngle = Mathf.Max(TakerConeAngle, Mathf.Atan2(BodyRadius, distance) * Mathf.Rad2Deg) * slack;
            float score = smallestAngle / allowedAngle;

            return score <= 1f && IsInLineOfSight(eyes, candidate, towardCandidate / distance, distance) ? score : float.PositiveInfinity;
        }

        private bool IsInLineOfSight(Transform eyes, Character candidate, Vector3 direction, float distance)
        {
            int hitCount = Interactor.CollectLookHits(eyes.position, direction, distance, eyes.root, lineOfSightHits);
            Transform candidateRoot = candidate.transform.root;

            for (int i = 0; i < hitCount; i++)
            {
                if (!lineOfSightHits[i].transform.IsChildOf(candidateRoot) && lineOfSightHits[i].distance < distance - BodyRadius) return false;
            }

            return true;
        }

        private static VisualElement CreateOneCellItemElement(ItemAsset asset, int row, int column)
        {
            VisualElement itemElement = CreateSlotElement(asset, asset.Id, false, true);
            itemElement.style.position = Position.Absolute;
            itemElement.style.left = column * CellSize;
            itemElement.style.top = row * CellSize;
            itemElement.style.width = CellSize;
            itemElement.style.height = CellSize;

            return itemElement;
        }

        private static void AddAmountLabel(VisualElement itemElement, int count)
        {
            Label amountLabel = new(count.ToString());
            amountLabel.AddToClassList("slot__amount");
            itemElement.Add(amountLabel);
        }

        // The bounding box of the filled cells against the recipe's, so a one-cell recipe matches in any cell
        private CraftAsset FindMatchingCraft()
        {
            if (crafter == null || inventory == null) return null;

            StackableItemAsset[] craftGridAssets = new StackableItemAsset[craftGridStacks.Length];
            for (int i = 0; i < craftGridStacks.Length; i++)
            {
                craftGridAssets[i] = craftGridStacks[i].Asset;
            }

            if (!TryCropToFilledCells(craftGridAssets, out StackableItemAsset[] placedShape, out int width, out int height)) return null;

            foreach (CraftAsset craft in crafter.AvailableCrafts)
            {
                if (craft == null || craft.Output == null || craft.Input == null || craft.Input.Length != craftGridStacks.Length) continue;
                if (!TryCropToFilledCells(craft.Input, out StackableItemAsset[] recipeShape, out int recipeWidth, out int recipeHeight)) continue;
                if (recipeWidth != width || recipeHeight != height) continue;

                bool isSameShape = true;
                for (int i = 0; i < placedShape.Length && isSameShape; i++)
                {
                    isSameShape = placedShape[i] == recipeShape[i];
                }

                if (isSameShape) return craft;
            }

            return null;
        }

        private static bool TryCropToFilledCells(StackableItemAsset[] cells, out StackableItemAsset[] shape, out int width, out int height)
        {
            int top = CraftGridSide, left = CraftGridSide, bottom = -1, right = -1;
            for (int i = 0; i < cells.Length; i++)
            {
                if (cells[i] == null) continue;

                int row = i / CraftGridSide;
                int column = i % CraftGridSide;
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
            {
                for (int column = 0; column < width; column++)
                {
                    shape[row * width + column] = cells[(top + row) * CraftGridSide + left + column];
                }
            }

            return true;
        }

        private int FindCraftGridCellAt(Vector2 position)
        {
            Rect bounds = craftGridElement.worldBound;
            if (!bounds.Contains(position)) return -1;

            int column = Mathf.Clamp((int)((position.x - bounds.x) / CellSize), 0, CraftGridSide - 1);
            int row = Mathf.Clamp((int)((position.y - bounds.y) / CellSize), 0, CraftGridSide - 1);

            return row * CraftGridSide + column;
        }

        private static void DrawGridCells(VisualElement gridElement, int rows)
        {
            DrawGridCells(gridElement, rows, InventoryGridColumns);
        }

        private static void DrawGridCells(VisualElement gridElement, int rows, int columns)
        {
            gridElement.style.width = columns * CellSize;
            gridElement.style.height = rows * CellSize;

            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    VisualElement cellElement = new();
                    cellElement.AddToClassList("grid__cell");
                    cellElement.style.left = column * CellSize;
                    cellElement.style.top = row * CellSize;
                    cellElement.style.width = CellSize;
                    cellElement.style.height = CellSize;
                    gridElement.Add(cellElement);
                }
            }
        }

        private static bool TryPlaceInGrid(bool[,] takenCells, ItemAsset asset, int rows, int columns, out int row, out int column)
        {
            Vector2Int size = asset == null ? Vector2Int.one : asset.SizeInCells;

            for (row = 0; row + size.y <= rows; row++)
            {
                for (column = 0; column + size.x <= columns; column++)
                {
                    if (!AreCellsFree(takenCells, row, column, size)) continue;

                    TakeCells(takenCells, row, column, size);

                    return true;
                }
            }

            row = 0;
            column = 0;

            return false;
        }

        private static bool AreCellsFree(bool[,] takenCells, int row, int column, Vector2Int size)
        {
            for (int down = 0; down < size.y; down++)
            {
                for (int right = 0; right < size.x; right++)
                {
                    if (takenCells[row + down, column + right]) return false;
                }
            }

            return true;
        }

        private static void TakeCells(bool[,] takenCells, int row, int column, Vector2Int size)
        {
            for (int down = 0; down < size.y; down++)
            {
                for (int right = 0; right < size.x; right++)
                {
                    takenCells[row + down, column + right] = true;
                }
            }
        }

        private VisualElement CreateItemElement(ItemAsset asset, string fallbackTitle, int row, int column, string amountText, int slot, bool equipable, bool inHands, StackableItemAsset stackableAsset = null, int splitIndex = -1)
        {
            Vector2Int sizeInCells = asset == null ? Vector2Int.one : asset.SizeInCells;
            Vector2 size = new(sizeInCells.x * CellSize, sizeInCells.y * CellSize);
            VisualElement itemElement = CreateSlotElement(asset, fallbackTitle, inHands, equipable);

            itemElement.style.position = Position.Absolute;
            itemElement.style.left = column * CellSize;
            itemElement.style.top = row * CellSize;
            itemElement.style.width = size.x;
            itemElement.style.height = size.y;

            if (amountText != null)
            {
                Label amountLabel = new(amountText);
                amountLabel.AddToClassList("slot__amount");
                itemElement.Add(amountLabel);
            }

            if (stackableAsset != null)
            {
                MakeDraggable(itemElement, GetIconSprite(asset), size, () =>
                {
                    draggedStackableAsset = stackableAsset;
                    draggedSplitIndex = splitIndex;
                });
            }
            else
            {
                MakeDraggable(itemElement, GetIconSprite(asset), size, () =>
                {
                    draggedUniqueSlot = slot;
                    uniqueItemDragged = true;
                    draggedItemEquipable = equipable;
                    draggedFromHands = inHands;
                });
            }

            return itemElement;
        }

        private static Sprite GetIconSprite(ItemAsset asset)
        {
            return asset == null || asset.Icon == null ? null : asset.Icon.Sprite;
        }

        private void MakeDraggable(VisualElement itemElement, Sprite iconSprite, Vector2 size, Action beginDrag)
        {
            itemElement.RegisterCallback<PointerDownEvent>(pointerDown =>
            {
                if (pointerDown.button != 0 || dragGhostElement != null) return;

                beginDrag();
                dragPointerId = pointerDown.pointerId;

                dragGhostElement = CreateDragGhost(iconSprite, size);
                windowElement.Add(dragGhostElement);
                MoveDragGhost(pointerDown.position, size);

                itemElement.CapturePointer(dragPointerId);
                pointerDown.StopPropagation();
            });

            itemElement.RegisterCallback<PointerMoveEvent>(pointerMove =>
            {
                if (dragGhostElement == null || pointerMove.pointerId != dragPointerId) return;

                MoveDragGhost(pointerMove.position, size);
            });

            itemElement.RegisterCallback<PointerUpEvent>(pointerUp =>
            {
                if (dragGhostElement == null || pointerUp.pointerId != dragPointerId) return;

                itemElement.ReleasePointer(dragPointerId);
                EndDrag(pointerUp.position);
            });
        }

        private static VisualElement CreateDragGhost(Sprite iconSprite, Vector2 size)
        {
            VisualElement ghostElement = new();
            ghostElement.AddToClassList("ghost");
            ghostElement.style.width = size.x;
            ghostElement.style.height = size.y;

            if (iconSprite != null) ghostElement.style.backgroundImage = Background.FromSprite(iconSprite);

            return ghostElement;
        }

        private void MoveDragGhost(Vector2 position, Vector2 size)
        {
            dragGhostElement.style.left = position.x - size.x / 2f;
            dragGhostElement.style.top = position.y - size.y / 2f;
        }

        private void EndDrag(Vector2 position)
        {
            dragGhostElement.RemoveFromHierarchy();
            dragGhostElement = null;

            if (inventory != null) DropDraggedAt(position);

            draggedStackableAsset = null;
            draggedSplitIndex = -1;
            draggedCraftGridCell = -1;
            draggedOfferIndex = -1;
            craftOutputDragged = false;
            uniqueItemDragged = false;
            draggedItemEquipable = false;
        }

        private void DropDraggedAt(Vector2 position)
        {
            bool isOverInventory = inventoryGridElement.worldBound.Contains(position) || handsGridElement.worldBound.Contains(position);
            bool isOverGivePanel = chosenTaker != null && givePanelElement.worldBound.Contains(position);
            int craftGridCell = FindCraftGridCellAt(position);

            if (craftOutputDragged)
            {
                CraftAsset matchingCraft = FindMatchingCraft();
                if (matchingCraft == null || !isOverInventory) return;

                // The craft grid pays when the inventory confirms the result arrived
                pendingCraft = matchingCraft;
                outputCountBeforeCraft = CountInInventory(matchingCraft.Output);
                crafter.CraftRpc(matchingCraft.NetworkId);
                return;
            }

            if (draggedOfferIndex >= 0)
            {
                if (!isOverGivePanel && draggedOfferIndex < offeredItems.Count) offeredItems.RemoveAt(draggedOfferIndex);

                stale = true;
                return;
            }

            if (draggedCraftGridCell >= 0)
            {
                ItemStack movedStack = craftGridStacks[draggedCraftGridCell];

                if (isOverGivePanel)
                {
                    if (movedStack.Asset != null && TryOfferStackable(movedStack.Asset, movedStack.Count)) craftGridStacks[draggedCraftGridCell] = default;
                }
                else if (craftGridCell < 0)
                {
                    craftGridStacks[draggedCraftGridCell] = default;
                }
                else if (craftGridCell != draggedCraftGridCell && craftGridStacks[craftGridCell].Asset == null)
                {
                    craftGridStacks[craftGridCell] = movedStack;
                    craftGridStacks[draggedCraftGridCell] = default;
                }
                else if (craftGridCell != draggedCraftGridCell && craftGridStacks[craftGridCell].Asset == movedStack.Asset)
                {
                    craftGridStacks[craftGridCell].Count += movedStack.Count;
                    craftGridStacks[draggedCraftGridCell] = default;
                }

                stale = true;
                return;
            }

            if (draggedStackableAsset != null)
            {
                bool isSplit = draggedSplitIndex >= 0 && draggedSplitIndex < splitStacks.Count;
                int amount = isSplit ? splitStacks[draggedSplitIndex].Count : inventory.CountStackable(draggedStackableAsset) - CountReserved(draggedStackableAsset);
                if (amount <= 0) return;

                bool isMoved = isOverGivePanel ? TryOfferStackable(draggedStackableAsset, amount) : craftGridCell >= 0 && TryPutOnCraftGrid(craftGridCell, draggedStackableAsset, amount);
                if (!isMoved) return;

                if (isSplit) splitStacks.RemoveAt(draggedSplitIndex);
                stale = true;
                return;
            }

            if (!uniqueItemDragged) return;

            if (isOverGivePanel)
            {
                if (TryOfferUnique(draggedUniqueSlot)) stale = true;
                return;
            }

            if (!draggedItemEquipable) return;

            if (handsGridElement.worldBound.Contains(position) && !draggedFromHands)
            {
                inventoryHolder.HoldSlotRpc(draggedUniqueSlot);
            }
            else if (inventoryGridElement.worldBound.Contains(position) && draggedFromHands)
            {
                inventoryHolder.HoldSlotRpc(Inventory.NoSlot);
            }
        }

        private bool TryPutOnCraftGrid(int cell, StackableItemAsset asset, int amount)
        {
            if (craftGridStacks[cell].Asset == null)
            {
                craftGridStacks[cell] = new ItemStack { Asset = asset, Count = amount };
            }
            else if (craftGridStacks[cell].Asset == asset)
            {
                craftGridStacks[cell].Count += amount;
            }
            else
            {
                return false;
            }

            return true;
        }

        private void AddContextMenu(VisualElement itemElement, StackableItemAsset stackableAsset, int amount, int slot)
        {
            itemElement.RegisterCallback<PointerDownEvent>(pointerDown =>
            {
                if (pointerDown.button != 1 || dragGhostElement != null) return;

                OpenContextMenu(pointerDown.position, stackableAsset, amount, slot);
                pointerDown.StopPropagation();
            });
        }

        private void OpenContextMenu(Vector2 position, StackableItemAsset stackableAsset, int amount, int slot)
        {
            CloseContextMenu();

            contextMenuCurtainElement = new VisualElement();
            contextMenuCurtainElement.AddToClassList("menu-curtain");
            contextMenuCurtainElement.RegisterCallback<PointerDownEvent>(pointerDown =>
            {
                if (pointerDown.target == contextMenuCurtainElement) CloseContextMenu();
            });

            VisualElement menuElement = new();
            menuElement.AddToClassList("context-menu");

            Vector2 localPosition = windowElement.WorldToLocal(position);
            menuElement.style.left = localPosition.x;
            menuElement.style.top = localPosition.y;

            if (stackableAsset != null && stackableAsset.IsUsable) AddContextMenuItem(menuElement, "Использовать", () => inventoryUser.UseItemRpc(stackableAsset.NetworkId));
            if (stackableAsset != null && amount > 1) AddContextMenuItem(menuElement, "Разделить", () => OpenSplitPrompt(stackableAsset));

            if (menuElement.childCount == 0) return;

            contextMenuCurtainElement.Add(menuElement);
            windowElement.Add(contextMenuCurtainElement);
        }

        private void AddContextMenuItem(VisualElement menuElement, string text, Action action)
        {
            Button itemButton = new(() =>
            {
                if (inventory != null) action();

                CloseContextMenu();
            }) { text = text };
            itemButton.AddToClassList("context-menu__item");
            menuElement.Add(itemButton);
        }

        private void CloseContextMenu()
        {
            if (contextMenuCurtainElement == null) return;

            contextMenuCurtainElement.RemoveFromHierarchy();
            contextMenuCurtainElement = null;
        }

        // While the number is typed the player's keys belong to the field: Escape closes only this box
        private void OpenSplitPrompt(StackableItemAsset asset)
        {
            LocalPlayer localPlayer = OwnPlayer.FindComponent<LocalPlayer>();
            if (localPlayer == null) return;

            HideSplitPrompt();
            localPlayer.OpenPrompt();

            splitPromptElement = new VisualElement();
            splitPromptElement.AddToClassList("menu-curtain");
            splitPromptElement.AddToClassList("split-curtain");
            splitPromptElement.RegisterCallback<PointerDownEvent>(pointerDown =>
            {
                if (pointerDown.target == splitPromptElement) CloseSplitPrompt();
            });

            VisualElement boxElement = new();
            boxElement.AddToClassList("split");

            VisualElement headElement = new();
            headElement.AddToClassList("split__head");

            Button closeButton = new(CloseSplitPrompt) { text = "×" };
            closeButton.AddToClassList("split__close");
            headElement.Add(closeButton);

            Label titleLabel = new($"Разделить: {asset.Title}");
            titleLabel.AddToClassList("line");
            titleLabel.AddToClassList("split__title");
            headElement.Add(titleLabel);
            boxElement.Add(headElement);

            int freeCount = inventory.CountStackable(asset) - CountReserved(asset);
            TextField amountField = new() { maxLength = SplitAmountMaxDigits, value = Math.Max(1, freeCount / 2).ToString() };
            amountField.AddToClassList("split__field");
            amountField.RegisterValueChangedCallback(change =>
            {
                string digits = KeepDigits(change.newValue);
                if (digits != change.newValue) amountField.SetValueWithoutNotify(digits);
            });
            amountField.RegisterCallback<KeyDownEvent>(keyDown =>
            {
                if (keyDown.keyCode != KeyCode.Return && keyDown.keyCode != KeyCode.KeypadEnter) return;

                keyDown.StopPropagation();
                SplitStack(asset, amountField.value);
            });
            boxElement.Add(amountField);

            Button confirmButton = new(() => SplitStack(asset, amountField.value)) { text = "Разделить" };
            confirmButton.AddToClassList("split__button");
            boxElement.Add(confirmButton);

            splitPromptElement.Add(boxElement);
            windowElement.Add(splitPromptElement);
            amountField.schedule.Execute(() =>
            {
                amountField.Focus();
                amountField.SelectAll();
            });
        }

        private void SplitStack(StackableItemAsset asset, string typedAmount)
        {
            int freeCount = inventory == null ? 0 : inventory.CountStackable(asset) - CountReserved(asset);
            if (!int.TryParse(typedAmount, out int count) || count <= 0 || count >= freeCount) return;

            splitStacks.Add(new ItemStack { Asset = asset, Count = count });
            CloseSplitPrompt();
            stale = true;
            Log.Info($"Split {count} of {freeCount} {asset.Id} off in the bag");
        }

        private static string KeepDigits(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            StringBuilder digits = new(text.Length);
            foreach (char character in text)
            {
                if (character >= '0' && character <= '9') digits.Append(character);
            }

            return digits.ToString();
        }

        private void CloseSplitPrompt()
        {
            if (splitPromptElement == null) return;

            OwnPlayer.FindComponent<LocalPlayer>()?.ClosePrompt();
            HideSplitPrompt();
        }

        private void HideSplitPrompt()
        {
            if (splitPromptElement == null) return;

            splitPromptElement.RemoveFromHierarchy();
            splitPromptElement = null;
        }

        private static VisualElement CreateSlotElement(ItemAsset asset, string fallbackTitle, bool inHands, bool equipable)
        {
            Vector2Int sizeInCells = asset == null ? Vector2Int.one : asset.SizeInCells;

            VisualElement slotElement = new() { tooltip = asset == null ? fallbackTitle : asset.Title };
            slotElement.AddToClassList("slot");
            if (inHands) slotElement.AddToClassList("slot--held");
            if (!equipable) slotElement.AddToClassList("slot--fixed");

            if (GetIconSprite(asset) != null)
            {
                VisualElement iconElement = new();
                iconElement.AddToClassList("slot__icon");
                iconElement.style.backgroundImage = Background.FromSprite(GetIconSprite(asset));
                iconElement.style.width = sizeInCells.x * CellSize - IconBezel;
                iconElement.style.height = sizeInCells.y * CellSize - IconBezel;
                slotElement.Add(iconElement);
            }
            else
            {
                Label nameLabel = new(asset == null ? fallbackTitle : asset.Title);
                nameLabel.AddToClassList("slot__name");
                slotElement.Add(nameLabel);
            }

            return slotElement;
        }
    }
}
