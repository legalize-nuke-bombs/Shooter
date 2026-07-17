using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Client.Menu
{
    public class CreateWorldModal
    {
        private static readonly string[] PolicyLabels = { "Открытый: вход по идентификатору", "Закрытый: новые участники не принимаются" };
        private static readonly string[] PolicyValues = { "EVERYONE", "NOBODY" };

        private readonly VisualElement modal;
        private readonly TextField nameField;
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
            policy = root.Q<DropdownField>("create-policy");
            status = root.Q<Label>("create-status");

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
            if (name.Length < 1) { status.text = "Укажите название мира."; return; }

            status.text = "";
            busy = true;

            api.CreateWorld(new CreateWorldRequest
            {
                name = name,
                joinPolicy = PolicyValues[Mathf.Clamp(policy.index, 0, PolicyValues.Length - 1)]
            }, error =>
            {
                busy = false;
                if (error != null) { status.text = error; return; }

                Hide();
                nameField.value = "";
                onCreated();
            });
        }
    }
}
