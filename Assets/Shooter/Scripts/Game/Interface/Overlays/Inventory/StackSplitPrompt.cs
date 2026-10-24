using System;
using System.Collections.Generic;
using System.Text;
using Shooter.Infrastructure;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Game
{
    public sealed class StackSplitPrompt
    {
        private const int AmountMaxDigits = 7;
        private static readonly Journal Log = Logs.Here();

        private readonly List<ItemStack> piles = new();
        private readonly VisualElement windowElement;
        private readonly Action markStale;
        private VisualElement promptElement;

        public StackSplitPrompt(VisualElement windowElement, Action markStale)
        {
            this.windowElement = windowElement;
            this.markStale = markStale;
        }

        public IReadOnlyList<ItemStack> Piles => piles;

        public bool IsOpen => promptElement != null;

        public int CountOf(StackableItemAsset asset)
        {
            int count = 0;
            foreach (ItemStack pile in piles)
            {
                if (pile.Asset == asset) count += pile.Count;
            }

            return count;
        }

        public void RemovePileAt(int index)
        {
            piles.RemoveAt(index);
        }

        public void ReleaseUnbacked(Inventory inventory, Func<StackableItemAsset, int> countReserved)
        {
            for (int i = piles.Count - 1; i >= 0; i--)
            {
                ItemStack pile = piles[i];
                int excess = countReserved(pile.Asset) - inventory.CountStackable(pile.Asset);
                if (excess <= 0) continue;

                pile.Count -= Math.Min(excess, pile.Count);
                if (pile.Count <= 0)
                {
                    piles.RemoveAt(i);
                }
                else
                {
                    piles[i] = pile;
                }
            }
        }

        public void Clear()
        {
            piles.Clear();
        }

        // While the number is typed the player's keys belong to the field: Escape closes only this box
        public void Open(StackableItemAsset asset, Func<int> countFree)
        {
            PlayerController playerController = LocalPlayerFinder.FindComponent<PlayerController>();
            if (playerController == null) return;

            Hide();
            playerController.OpenPrompt();

            promptElement = new VisualElement();
            promptElement.AddToClassList("menu-curtain");
            promptElement.AddToClassList("split-curtain");
            promptElement.RegisterCallback<PointerDownEvent>(pointerDown =>
            {
                if (pointerDown.target == promptElement) Close();
            });

            VisualElement boxElement = new();
            boxElement.AddToClassList("split");

            VisualElement headElement = new();
            headElement.AddToClassList("split__head");

            Button closeButton = new(Close) { text = "×" };
            closeButton.AddToClassList("split__close");
            headElement.Add(closeButton);

            Label titleLabel = new($"Разделить: {asset.Title}");
            titleLabel.AddToClassList("line");
            titleLabel.AddToClassList("split__title");
            headElement.Add(titleLabel);
            boxElement.Add(headElement);

            TextField amountField = new() { maxLength = AmountMaxDigits, value = Math.Max(1, countFree() / 2).ToString() };
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
                Split(asset, amountField.value, countFree());
            });
            boxElement.Add(amountField);

            Button confirmButton = new(() => Split(asset, amountField.value, countFree())) { text = "Разделить" };
            confirmButton.AddToClassList("split__button");
            boxElement.Add(confirmButton);

            promptElement.Add(boxElement);
            windowElement.Add(promptElement);
            amountField.schedule.Execute(() =>
            {
                amountField.Focus();
                amountField.SelectAll();
            });
        }

        public void Close()
        {
            if (promptElement == null) return;

            LocalPlayerFinder.FindComponent<PlayerController>()?.ClosePrompt();
            Hide();
        }

        public void Hide()
        {
            if (promptElement == null) return;

            promptElement.RemoveFromHierarchy();
            promptElement = null;
        }

        private void Split(StackableItemAsset asset, string typedAmount, int freeCount)
        {
            if (!int.TryParse(typedAmount, out int count) || count <= 0 || count >= freeCount) return;

            piles.Add(new ItemStack { Asset = asset, Count = count });
            Close();
            markStale();
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
    }
}
