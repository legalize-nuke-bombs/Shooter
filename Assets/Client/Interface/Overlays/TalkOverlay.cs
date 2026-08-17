using Shooter.Client.Playing;
using Shooter.Game.Body;
using Shooter.Game.Speech;
using Shooter.Logging;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Client.Interface
{
    public class TalkOverlay : Overlay
    {
        private const string WindowElement = "talk";
        private const string NameElement = "talk-name";
        private const string LogElement = "talk-log";
        private const string WaitingElement = "talk-waiting";
        private const string InputElement = "talk-input";
        private const string Stranger = "Незнакомец";
        private static readonly Journal Log = Logs.Here();

        private readonly NameMapper mapper = new();
        private TextField input;
        private ScrollView log;
        private Mouth mouth;
        private Label speaker;
        private Label waiting;

        private VisualElement window;

        private void Update()
        {
            if (!Bound) return;

            Mouth own = OwnPlayer.Find<Mouth>();
            if (own == mouth) return;

            Forget();
            mouth = own;

            if (mouth == null) return;

            mouth.Opened += Open;
            mouth.Heard += Line;
            mouth.Closed += Close;
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

            input.maxLength = Talker.SpeechLimit;
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
            log.Clear();
            Wait(false);
            input.value = string.Empty;
            speaker.text = Named(talkerId);
            window.style.display = DisplayStyle.Flex;

            input.Focus();
            Log.Info($"Talk window opened with {speaker.text}");
        }

        private void Line(string content, string time, bool mine)
        {
            var line = new Label(content);
            line.AddToClassList("talk__line");
            if (mine) line.AddToClassList("talk__line--mine");

            log.Add(line);
            log.schedule.Execute(() => log.ScrollTo(line));

            Wait(mine);
        }

        private void Close()
        {
            window.style.display = DisplayStyle.None;
            log.Clear();
            Wait(false);
            input.value = string.Empty;

            Log.Info("Talk window closed");
        }

        private void Wait(bool answering)
        {
            waiting.style.display = answering ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void Typed(KeyDownEvent typed)
        {
            if (typed.keyCode != KeyCode.Return && typed.keyCode != KeyCode.KeypadEnter) return;

            string speech = input.value.Trim();
            input.value = string.Empty;
            typed.StopPropagation();

            if (speech.Length == 0 || mouth == null) return;

            mouth.SayRpc(speech);
        }

        private string Named(ulong talkerId)
        {
            NetworkManager network = NetworkManager.Singleton;
            if (network == null || network.SpawnManager == null) return Stranger;

            if (!network.SpawnManager.SpawnedObjects.TryGetValue(talkerId, out NetworkObject talker)) return Stranger;

            Nameable nameable = talker.GetComponentInChildren<Nameable>();
            if (nameable == null) return Stranger;

            string named = mapper.Of(nameable);

            return string.IsNullOrEmpty(named) ? Stranger : named;
        }

        private void Forget()
        {
            if (mouth == null) return;

            mouth.Opened -= Open;
            mouth.Heard -= Line;
            mouth.Closed -= Close;
            mouth = null;
        }
    }
}
