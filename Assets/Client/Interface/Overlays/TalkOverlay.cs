using System.Collections;
using Shooter.Client.Playing;
using Shooter.Game.Body;
using Shooter.Game.Core;
using Shooter.Game.Speech;
using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Client.Interface
{
    // A view over the player's mirror of conversations: one partner's lines on screen,
    // everything known drawn at once, only what arrives while the window is open gets typed out
    public class TalkOverlay : Overlay
    {
        private const string WindowElement = "talk";
        private const string NameElement = "talk-name";
        private const string LogElement = "talk-log";
        private const string WaitingElement = "talk-waiting";
        private const string InputElement = "talk-input";
        private const string Stranger = "Незнакомец";
        private const string Radio = "рация";
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private float charInterval = 0.01f;

        private readonly NameMapper mapper = new();
        private TextField input;
        private ScrollView log;
        private Label speaker;
        private Label waiting;
        private VisualElement window;

        private PlayerMouth playerMouth;
        private PlayerConversations conversations;
        private PlayerRadio playerRadio;
        private Talker talker;

        // The pair on screen, null while the window is closed, and how many of its lines are drawn
        private Contact contact;
        private int drawn;

        // Over the radio the window shows whoever the local player picked, face to face whoever the server opened
        private bool radio;
        private long? radioShown;

        private void Update()
        {
            if (!Bound) return;

            Follow();
            PollRadio();
        }

        private void PollRadio()
        {
            LocalPlayer player = OwnPlayer.Find<LocalPlayer>();
            long? wanted = player == null ? null : player.RadioPartner;
            if (wanted == radioShown) return;

            radioShown = wanted;
            if (wanted != null) OpenRadio(wanted.Value);
            else if (radio) Close();
        }

        private void Follow()
        {
            PlayerMouth own = OwnPlayer.Find<PlayerMouth>();
            if (own == playerMouth) return;

            Forget();
            playerMouth = own;

            if (playerMouth == null) return;

            playerMouth.Opened += Open;
            playerMouth.Closed += Close;

            playerRadio = playerMouth.GetComponent<PlayerRadio>();
            conversations = playerMouth.GetComponent<PlayerConversations>();
            if (conversations == null)
            {
                Log.Error($"Player {playerMouth.name} has no conversations, talks stay empty");
                return;
            }

            conversations.Arrived += Arrived;
            conversations.Cleared += Cleared;
        }

        protected override bool Bind(VisualElement root)
        {
            window = root.Q<VisualElement>(WindowElement);
            speaker = root.Q<Label>(NameElement);
            log = root.Q<ScrollView>(LogElement);
            waiting = root.Q<Label>(WaitingElement);
            input = root.Q<TextField>(InputElement);

            if (window == null || speaker == null || log == null || waiting == null || input == null)
            {
                Log.Error($"Overlay document has no {WindowElement} window, talks stay invisible");
                return false;
            }

            input.maxLength = PlayerMouth.SpeechLimit;
            input.RegisterCallback<KeyDownEvent>(Typed);
            window.style.display = DisplayStyle.None;

            return true;
        }

        protected override void Unbind()
        {
            Forget();
            window = null;
        }

        private void Open(ulong talkerId)
        {
            Talker found = TalkerOf(talkerId);
            if (found == null || conversations == null)
            {
                Log.Warn($"Talk with network object {talkerId} opened, but the talker or the mirror is missing");
                return;
            }

            radio = false;
            Show(conversations.ContactOf(found.CharacterId), found, Named(found));
        }

        private void OpenRadio(long partnerId)
        {
            if (conversations == null) return;

            Character partner = Character.Of(partnerId, Inactive.Exclude);
            radio = true;
            Show(conversations.ContactOf(partnerId), partner == null ? null : partner.GetComponent<Talker>(),
                $"{Named(partner)} ({Radio})");
        }

        private void Show(Contact pair, Talker with, string title)
        {
            Unfollow();

            contact = pair;
            speaker.text = title;
            input.value = string.Empty;
            window.style.display = DisplayStyle.Flex;

            Fill();
            Follow(with);

            input.Focus();
            Log.Info($"Talk window opened with {speaker.text}");
        }

        private void Close()
        {
            if (contact == null) return;

            Unfollow();
            contact = null;
            radio = false;
            drawn = 0;
            log.Clear();
            window.style.display = DisplayStyle.None;
            Wait(false);
            input.value = string.Empty;

            Log.Info("Talk window closed");
        }

        // Everything known goes on screen at once
        private void Fill()
        {
            log.Clear();
            drawn = 0;
            Draw(false);
        }

        private void Arrived(long partnerId)
        {
            if (contact == null || partnerId != contact.PartnerId) return;

            Draw(true);
        }

        private void Cleared()
        {
            Close();
        }

        // Lines go on screen in index order and stop at the first gap: whatever is still on its way
        // will be drawn when it lands, so the log never shows a later line above an earlier one
        private void Draw(bool typing)
        {
            while (drawn < contact.Count && contact.TryGet(drawn, out Line line))
            {
                Put(line, typing);
                drawn++;
            }

            contact.Seen = contact.Count;
        }

        private void Put(Line line, bool typing)
        {
            bool mine = line.AuthorId == conversations.CharacterId;

            var label = new Label();
            label.AddToClassList("talk__line");
            if (mine) label.AddToClassList("talk__line--mine");
            if (!line.Spoken) label.AddToClassList("talk__line--radio");

            log.Add(label);

            if (mine || !typing) label.text = line.Content;
            else StartCoroutine(Type(label, line.Content));

            log.schedule.Execute(() => log.ScrollTo(label));
        }

        private IEnumerator Type(Label line, string content)
        {
            for (int i = 1; i <= content.Length; i++)
            {
                line.text = content.Substring(0, i);
                log.ScrollTo(line);
                yield return new WaitForSeconds(charInterval);
            }
        }

        private void Follow(Talker with)
        {
            if (with == null)
            {
                Wait(false);
                return;
            }

            talker = with;
            talker.ThinkingChanged += Wait;
            Wait(talker.Thinking);
        }

        private void Unfollow()
        {
            if (talker == null) return;

            talker.ThinkingChanged -= Wait;
            talker = null;
        }

        private void Wait(bool thinking)
        {
            waiting.style.display = thinking ? DisplayStyle.Flex : DisplayStyle.None;
            input.SetEnabled(!thinking);
            if (!thinking && window.style.display == DisplayStyle.Flex) input.Focus();
        }

        private void Typed(KeyDownEvent typed)
        {
            if (typed.keyCode != KeyCode.Return && typed.keyCode != KeyCode.KeypadEnter) return;

            string speech = input.value.Trim();
            input.value = string.Empty;
            typed.StopPropagation();

            if (speech.Length == 0 || contact == null) return;

            if (radio && playerRadio != null) playerRadio.SayRpc(contact.PartnerId, speech);
            else playerMouth.SayRpc(speech);
        }

        private static Talker TalkerOf(ulong talkerId)
        {
            NetworkManager network = NetworkManager.Singleton;
            if (network == null || network.SpawnManager == null) return null;

            return network.SpawnManager.SpawnedObjects.TryGetValue(talkerId, out NetworkObject found)
                ? found.GetComponentInChildren<Talker>()
                : null;
        }

        private string Named(Component character)
        {
            Nameable nameable = character == null ? null : character.GetComponentInChildren<Nameable>();
            if (nameable == null) return Stranger;

            string named = mapper.Of(nameable);

            return string.IsNullOrEmpty(named) ? Stranger : named;
        }

        private void Forget()
        {
            Close();

            if (conversations != null)
            {
                conversations.Arrived -= Arrived;
                conversations.Cleared -= Cleared;
                conversations = null;
            }

            if (playerMouth == null) return;

            playerMouth.Opened -= Open;
            playerMouth.Closed -= Close;
            playerMouth = null;
            playerRadio = null;
        }
    }
}
