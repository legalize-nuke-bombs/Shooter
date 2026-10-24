using System;
using System.Collections.Generic;
using Shooter.Infrastructure;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Game
{
    public sealed class GivePanel
    {
        private const int GridColumns = 4;
        private const int GridRows = 3;
        private const float TakerConeAngle = 12f;
        private const float BodyRadius = 0.6f;
        private const float SentOfferTimeoutSeconds = 3f;
        private const float TakerSearchIntervalSeconds = 0.2f;
        private const float ChosenTakerSlack = 1.25f;
        private static readonly Journal Log = Logs.Here();

        // Points along a body from feet to head around its root, which sits at the middle of the body for players and residents alike
        private static readonly float[] BodyHeights = { -0.8f, -0.4f, 0f, 0.4f, 0.8f };

        private struct Offer
        {
            public ItemAsset Asset;
            public string AssetId;
            public int Count;
            public int Slot;
            public int ExpectedCountAfterGive;
            public float SentTime;

            public StackableItemAsset StackableAsset => Slot == Inventory.NoSlot ? Asset as StackableItemAsset : null;
        }

        private readonly List<Offer> offers = new();
        private readonly RaycastHit[] lineOfSightHits = new RaycastHit[16];
        private readonly VisualElement panelElement;
        private readonly Label titleLabel;
        private readonly VisualElement gridElement;
        private readonly Button giveButton;
        private readonly ItemDragging dragging;
        private readonly Action markStale;
        private Inventory inventory;
        private InventoryExchanger inventoryExchanger;
        private Character giverCharacter;
        private Character chosenTaker;
        private float nextTakerSearchTime;

        public GivePanel(VisualElement panelElement, Label titleLabel, VisualElement gridElement, Button giveButton, ItemDragging dragging, Action markStale)
        {
            this.panelElement = panelElement;
            this.titleLabel = titleLabel;
            this.gridElement = gridElement;
            this.giveButton = giveButton;
            this.dragging = dragging;
            this.markStale = markStale;

            panelElement.style.display = DisplayStyle.None;
            giveButton.clicked += Give;
        }

        public bool HasTaker => chosenTaker != null;

        public bool IsOpen => inventory != null;

        public void Unbind()
        {
            giveButton.clicked -= Give;
        }

        public void Open(Inventory openedInventory, InventoryExchanger openedExchanger, Character openedGiver)
        {
            inventory = openedInventory;
            inventoryExchanger = openedExchanger;
            giverCharacter = openedGiver;
            chosenTaker = FindTakerNearestToViewCenter(null);
            nextTakerSearchTime = Time.unscaledTime + TakerSearchIntervalSeconds;
        }

        public string DescribeTaker()
        {
            return chosenTaker == null ? null : chosenTaker.name;
        }

        public void Close()
        {
            inventory = null;
            inventoryExchanger = null;
            giverCharacter = null;
            chosenTaker = null;
            offers.Clear();
            panelElement.style.display = DisplayStyle.None;
            gridElement.Clear();
        }

        public bool IsOver(Vector2 position)
        {
            return chosenTaker != null && panelElement.worldBound.Contains(position);
        }

        public bool IsSlotOffered(int slot)
        {
            foreach (Offer offer in offers)
            {
                if (offer.Slot != Inventory.NoSlot && offer.Slot == slot) return true;
            }

            return false;
        }

        public int CountOf(StackableItemAsset asset)
        {
            int count = 0;
            foreach (Offer offer in offers)
            {
                if (offer.StackableAsset == asset) count += offer.Count;
            }

            return count;
        }

        public void Update()
        {
            if (Time.unscaledTime >= nextTakerSearchTime)
            {
                nextTakerSearchTime = Time.unscaledTime + TakerSearchIntervalSeconds;
                UpdateChosenTaker();
            }

            if (chosenTaker == null) return;

            bool hasUnsentOffer = false;
            for (int i = 0; i < offers.Count; i++)
            {
                Offer offer = offers[i];
                if (offer.SentTime <= 0f)
                {
                    hasUnsentOffer = true;
                    continue;
                }

                if (Time.unscaledTime - offer.SentTime <= SentOfferTimeoutSeconds) continue;

                Log.Info($"The bag never let {offer.AssetId} x {offer.Count} go, it is back in the offer");
                offer.SentTime = 0f;
                offers[i] = offer;
                markStale();
            }

            bool isTakerNear = giverCharacter != null && inventoryExchanger != null && Vector3.Distance(giverCharacter.transform.position, chosenTaker.transform.position) <= inventoryExchanger.Radius;
            giveButton.SetEnabled(isTakerNear && hasUnsentOffer);
        }

        public void ForgetGiven()
        {
            IReadOnlyList<UniqueItem> uniqueItems = inventory.UniqueItems;

            for (int i = offers.Count - 1; i >= 0; i--)
            {
                Offer offer = offers[i];
                StackableItemAsset stackableAsset = offer.StackableAsset;

                bool isGiven = stackableAsset == null
                    ? offer.Slot >= uniqueItems.Count || uniqueItems[offer.Slot] == null || uniqueItems[offer.Slot].AssetId != offer.AssetId
                    : offer.SentTime > 0f && inventory.CountStackable(stackableAsset) <= offer.ExpectedCountAfterGive;

                if (isGiven) offers.RemoveAt(i);
            }
        }

        public void ReleaseUnbacked(Func<StackableItemAsset, int> countReserved)
        {
            for (int i = offers.Count - 1; i >= 0; i--)
            {
                Offer offer = offers[i];
                StackableItemAsset stackableAsset = offer.StackableAsset;
                if (stackableAsset == null || offer.SentTime > 0f) continue;

                int excess = countReserved(stackableAsset) - inventory.CountStackable(stackableAsset);
                if (excess <= 0) continue;

                offer.Count -= Math.Min(excess, offer.Count);
                if (offer.Count <= 0)
                {
                    offers.RemoveAt(i);
                }
                else
                {
                    offers[i] = offer;
                }
            }
        }

        public void Draw()
        {
            gridElement.Clear();
            panelElement.style.display = chosenTaker == null ? DisplayStyle.None : DisplayStyle.Flex;
            if (chosenTaker == null) return;

            titleLabel.text = $"Передать: {NameMapper.GetShownName(chosenTaker)}";
            ItemGrid.DrawCells(gridElement, GridRows, GridColumns);

            GridPlacement placement = new(GridRows, GridColumns, false);

            for (int i = 0; i < offers.Count; i++)
            {
                Offer offer = offers[i];
                placement.TryPlace(ItemGrid.GetSizeInCells(offer.Asset), out int row, out int column);

                Vector2 size = ItemGrid.GetSize(offer.Asset);
                VisualElement itemElement = ItemGrid.CreateSlotElement(offer.Asset, offer.AssetId, false, offer.SentTime <= 0f);
                ItemGrid.PlaceAt(itemElement, row, column, size);

                if (offer.StackableAsset != null) ItemGrid.AddAmountLabel(itemElement, offer.Count);

                if (offer.SentTime > 0f)
                {
                    itemElement.AddToClassList("slot--sent");
                    gridElement.Add(itemElement);
                    continue;
                }

                int offerIndex = i;
                dragging.MakeDraggable(itemElement, offer.Asset, size, DraggedItem.Offer(offerIndex));
                itemElement.RegisterCallback<PointerDownEvent>(pointerDown =>
                {
                    if (pointerDown.button != 1 || dragging.IsDragging) return;

                    RemoveOffer(offerIndex);
                    markStale();
                    pointerDown.StopPropagation();
                });
                gridElement.Add(itemElement);
            }
        }

        public void RemoveOffer(int index)
        {
            if (index < offers.Count) offers.RemoveAt(index);
        }

        public bool TryOfferStackable(StackableItemAsset asset, int amount)
        {
            for (int i = 0; i < offers.Count; i++)
            {
                Offer offer = offers[i];
                if (offer.StackableAsset != asset || offer.SentTime > 0f) continue;

                offer.Count += amount;
                offers[i] = offer;
                return true;
            }

            if (!HasRoomFor(asset)) return false;

            offers.Add(new Offer { Asset = asset, AssetId = asset.Id, Count = amount, Slot = Inventory.NoSlot });
            return true;
        }

        public bool TryOfferUnique(int slot)
        {
            if (inventory == null || IsSlotOffered(slot)) return false;

            IReadOnlyList<UniqueItem> uniqueItems = inventory.UniqueItems;
            UniqueItem uniqueItem = slot >= 0 && slot < uniqueItems.Count ? uniqueItems[slot] : null;
            if (uniqueItem == null) return false;

            ItemCatalog itemCatalog = Catalogs.GetCatalog<ItemCatalog>();
            ItemAsset asset = itemCatalog == null ? null : itemCatalog.GetAssetById(uniqueItem.AssetId);
            if (!HasRoomFor(asset)) return false;

            offers.Add(new Offer { Asset = asset, AssetId = uniqueItem.AssetId, Count = 1, Slot = slot });
            return true;
        }

        private bool HasRoomFor(ItemAsset extraAsset)
        {
            GridPlacement placement = new(GridRows, GridColumns, false);

            foreach (Offer offer in offers)
            {
                if (!placement.TryPlace(ItemGrid.GetSizeInCells(offer.Asset), out _, out _)) return false;
            }

            return placement.TryPlace(ItemGrid.GetSizeInCells(extraAsset), out _, out _);
        }

        private void Give()
        {
            if (inventory == null || chosenTaker == null) return;

            long takerId = chosenTaker.Id;
            int sentCount = 0;

            for (int i = 0; i < offers.Count; i++)
            {
                Offer offer = offers[i];
                if (offer.SentTime > 0f) continue;

                StackableItemAsset stackableAsset = offer.StackableAsset;
                if (stackableAsset != null)
                {
                    offer.ExpectedCountAfterGive = inventory.CountStackable(stackableAsset) - offer.Count;
                    inventoryExchanger.GiveStackableRpc(takerId, stackableAsset.NetworkId, offer.Count);
                }
                else
                {
                    inventoryExchanger.GiveUniqueRpc(takerId, offer.Slot);
                }

                offer.SentTime = Time.unscaledTime;
                offers[i] = offer;
                sentCount++;
            }

            markStale();
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
            offers.RemoveAll(offer => offer.SentTime <= 0f);
            markStale();
        }

        // The one already chosen keeps a margin, so the panel does not blink at the edge of the cone or the radius
        private Character FindTakerNearestToViewCenter(Character currentTaker)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null || giverCharacter == null || inventory == null) return null;

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
            if (candidate == null || candidate == giverCharacter || !candidate.gameObject.activeInHierarchy) return float.PositiveInfinity;
            if (candidate.GetComponentInChildren<InventoryExchanger>() == null) return float.PositiveInfinity;

            Health health = candidate.GetComponent<Health>();
            if (health != null && !health.IsAlive) return float.PositiveInfinity;

            if (Vector3.Distance(giverCharacter.transform.position, candidate.transform.position) > inventoryExchanger.Radius * slack) return float.PositiveInfinity;

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
    }
}
