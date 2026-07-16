using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Menu
{
    public class CreateWorldModal
    {
        private static readonly string[] VisibilityLabels = { "Скрытый", "Публичный" };
        private static readonly string[] VisibilityValues = { "PRIVATE", "PUBLIC" };
        private static readonly string[] PolicyLabels = { "Закрытый", "Открытый" };
        private static readonly string[] PolicyValues = { "NOBODY", "EVERYONE" };

        private readonly VisualElement modal;
        private readonly TextField nameField;
        private readonly TextField descField;
        private readonly DropdownField visibility;
        private readonly DropdownField policy;
        private readonly Label status;

        private readonly MenuApi api;
        private readonly Action onCreated;

        private bool busy;

        public CreateWorldModal(VisualElement root, MenuApi api, Action onCreated)
        {
            this.api = api;
            this.onCreated = onCreated;

            modal = root.Q<VisualElement>("create-modal");
            nameField = root.Q<TextField>("create-name");
            descField = root.Q<TextField>("create-desc");
            visibility = root.Q<DropdownField>("create-visibility");
            policy = root.Q<DropdownField>("create-policy");
            status = root.Q<Label>("create-status");

            visibility.choices = new List<string>(VisibilityLabels);
            visibility.index = 0;
            policy.choices = new List<string>(PolicyLabels);
            policy.index = 0;

            root.Q<Button>("create-confirm").clicked += Submit;
            root.Q<Button>("create-cancel").clicked += Hide;
        }

        public void Show()
        {
            status.text = "";
            modal.RemoveFromClassList("hidden");
        }

        public void Hide() => modal.AddToClassList("hidden");

        private void Submit()
        {
            if (busy) return;

            string name = nameField.value.Trim();
            if (name.Length < 1) { status.text = "Сначала назови мир"; return; }

            status.text = "";
            busy = true;

            api.CreateWorld(new CreateWorldRequest
            {
                name = name,
                description = descField.value ?? "",
                visibilityPolicy = VisibilityValues[Mathf.Clamp(visibility.index, 0, VisibilityValues.Length - 1)],
                joinPolicy = PolicyValues[Mathf.Clamp(policy.index, 0, PolicyValues.Length - 1)]
            }, error =>
            {
                busy = false;
                if (error != null) { status.text = error; return; }

                Hide();
                nameField.value = "";
                descField.value = "";
                onCreated();
            });
        }
    }
}
