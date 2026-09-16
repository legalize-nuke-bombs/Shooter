using System.Collections.Generic;
using System.Linq;
using Shooter.Client.Playing;
using Shooter.Game.Body;
using Shooter.Game.Core;
using Shooter.Game.Speech;
using Shooter.Logging;
using UnityEngine.UIElements;

namespace Shooter.Client.Interface
{
    // The radio: everyone the player has ever exchanged a line with, latest talk first; a pick opens the talk window
    public class RadioOverlay : Overlay
    {
        private const string WindowElement = "radio";
        private const string ListElement = "radio-list";
        private const string EmptyElement = "radio-empty";
        private const string Stranger = "Незнакомец";
        private static readonly Journal Log = Logs.Here();

        private readonly NameMapper mapper = new();
        private PlayerConversations conversations;
        private Label empty;
        private ScrollView list;
        private bool open;
        private bool stale;
        private VisualElement window;

        private void Update()
        {
            if (!Bound) return;

            LocalPlayer player = OwnPlayer.Find<LocalPlayer>();
            bool wanted = player != null && player.RadioOpen;

            if (wanted != open)
            {
                open = wanted;

                if (open) Open(player);
                else Close();
            }

            if (open && stale) Fill(player);
        }

        protected override bool Bind(VisualElement root)
        {
            window = root.Q<VisualElement>(WindowElement);
            list = root.Q<ScrollView>(ListElement);
            empty = root.Q<Label>(EmptyElement);

            if (window == null || list == null || empty == null)
            {
                Log.Error($"Overlay document has no {WindowElement} window, the radio stays hidden");
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

        private void Open(LocalPlayer player)
        {
            conversations = player.GetComponent<PlayerConversations>();
            if (conversations != null) conversations.Arrived += Touch;

            window.style.display = DisplayStyle.Flex;
            stale = true;
            Log.Info("The radio is open");
        }

        private void Close()
        {
            if (conversations != null) conversations.Arrived -= Touch;
            conversations = null;

            if (window != null) window.style.display = DisplayStyle.None;
            list?.Clear();
            Log.Info("The radio is closed");
        }

        private void Touch(long partnerId)
        {
            stale = true;
        }

        private void Fill(LocalPlayer player)
        {
            stale = false;
            list.Clear();

            List<Contact> contacts = conversations == null
                ? new List<Contact>()
                : conversations.Contacts.Values.OrderByDescending(contact => contact.LastTime).ToList();

            empty.style.display = contacts.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;

            foreach (Contact contact in contacts)
            {
                long partnerId = contact.PartnerId;

                var item = new Button(() => player.OpenRadioTalk(partnerId)) { text = Named(partnerId) };
                item.AddToClassList("radio__item");
                if (contact.Unread > 0) item.AddToClassList("radio__item--unread");

                list.Add(item);
            }
        }

        private string Named(long partnerId)
        {
            Character partner = Character.Of(partnerId, Inactive.Exclude);
            Nameable nameable = partner == null ? null : partner.GetComponentInChildren<Nameable>();
            if (nameable == null) return Stranger;

            string named = mapper.Of(nameable);

            return string.IsNullOrEmpty(named) ? Stranger : named;
        }
    }
}
