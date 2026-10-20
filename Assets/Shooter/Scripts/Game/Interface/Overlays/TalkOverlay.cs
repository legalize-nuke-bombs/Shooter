using System.Collections;
using Shooter.Infrastructure;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Game
{
    public class TalkOverlay : Overlay
    {
        private const string WindowElementName = "talk";
        private const string PartnerNameLabelName = "talk-name";
        private const string LinesScrollViewName = "talk-log";
        private const string WaitingLabelName = "talk-waiting";
        private const string InputFieldName = "talk-input";
        private const string GiveButtonName = "talk-give";
        private const string Stranger = "Незнакомец";
        private const string RadioTitleSuffix = "рация";
        private static readonly Journal Log = Logs.Here();

        [SerializeField] private float charInterval = 0.01f;

        private Button giveButton;
        private TextField inputField;
        private ScrollView linesScrollView;
        private Label partnerNameLabel;
        private Label waitingLabel;
        private VisualElement windowElement;

        private PlayerMouth playerMouth;
        private PlayerConversations playerConversations;
        private PlayerRadio playerRadio;
        private Talker partnerTalker;

        private Contact shownContact;
        private int drawnLineCount;

        private bool talkingOverRadio;
        private long? shownRadioPartnerId;

        private void Update()
        {
            if (!IsBound) return;

            SubscribeToOwnPlayerIfChanged();
            OpenOrCloseRadioTalk();
        }

        private void OpenOrCloseRadioTalk()
        {
            LocalPlayer localPlayer = OwnPlayer.FindComponent<LocalPlayer>();
            long? radioPartnerId = localPlayer == null ? null : localPlayer.RadioPartnerId;
            if (radioPartnerId == shownRadioPartnerId) return;

            shownRadioPartnerId = radioPartnerId;
            if (radioPartnerId != null)
            {
                OpenRadioTalk(radioPartnerId.Value);
            }
            else if (talkingOverRadio)
            {
                CloseTalk();
            }
        }

        private void SubscribeToOwnPlayerIfChanged()
        {
            PlayerMouth ownPlayerMouth = OwnPlayer.FindComponent<PlayerMouth>();
            if (ownPlayerMouth == playerMouth) return;

            CloseTalkAndUnsubscribeFromOwnPlayer();
            playerMouth = ownPlayerMouth;

            if (playerMouth == null) return;

            playerMouth.TalkOpened += OpenTalk;
            playerMouth.TalkClosed += CloseTalk;

            playerRadio = playerMouth.GetComponent<PlayerRadio>();
            playerConversations = playerMouth.GetComponent<PlayerConversations>();
            if (playerConversations == null)
            {
                Log.Error($"Player {playerMouth.name} has no conversations, talks stay empty");
                return;
            }

            playerConversations.LinesArrived += HandleLinesArrived;
            playerConversations.ContactsCleared += HandleContactsCleared;
        }

        protected override bool TryBindElements(VisualElement root)
        {
            windowElement = root.Q<VisualElement>(WindowElementName);
            partnerNameLabel = root.Q<Label>(PartnerNameLabelName);
            linesScrollView = root.Q<ScrollView>(LinesScrollViewName);
            waitingLabel = root.Q<Label>(WaitingLabelName);
            inputField = root.Q<TextField>(InputFieldName);
            giveButton = root.Q<Button>(GiveButtonName);

            if (windowElement == null || partnerNameLabel == null || linesScrollView == null || waitingLabel == null || inputField == null || giveButton == null)
            {
                Log.Error($"Overlay document has no {WindowElementName} window, talks stay invisible");
                return false;
            }

            inputField.maxLength = PlayerMouth.MaxSpeechLength;
            inputField.RegisterCallback<KeyDownEvent>(HandleInputKeyDown);
            giveButton.clicked += OpenInventoryFromTalk;
            windowElement.style.display = DisplayStyle.None;

            return true;
        }

        protected override void UnbindElements()
        {
            CloseTalkAndUnsubscribeFromOwnPlayer();

            if (giveButton != null) giveButton.clicked -= OpenInventoryFromTalk;
            giveButton = null;
            windowElement = null;
        }

        private void OpenTalk(ulong talkerNetworkObjectId)
        {
            Talker foundTalker = FindTalkerByNetworkObjectId(talkerNetworkObjectId);
            if (foundTalker == null || playerConversations == null)
            {
                Log.Warn($"Talk with network object {talkerNetworkObjectId} opened, but the talker or the mirror is missing");
                return;
            }

            talkingOverRadio = false;
            ShowTalk(playerConversations.GetOrCreateContact(foundTalker.CharacterId), foundTalker, GetPartnerName(foundTalker));
        }

        private void OpenRadioTalk(long partnerId)
        {
            if (playerConversations == null) return;

            Character partner = Character.FindById(partnerId, RegisterInactivePolicy.Exclude);
            talkingOverRadio = true;
            ShowTalk(playerConversations.GetOrCreateContact(partnerId), partner == null ? null : partner.GetComponent<Talker>(), $"{GetPartnerName(partner)} ({RadioTitleSuffix})");
        }

        private void ShowTalk(Contact contact, Talker talker, string title)
        {
            StopWatchingPartnerThinking();

            shownContact = contact;
            partnerNameLabel.text = title;
            giveButton.style.display = talkingOverRadio ? DisplayStyle.None : DisplayStyle.Flex;
            inputField.value = string.Empty;
            windowElement.style.display = DisplayStyle.Flex;

            RedrawAllLines();
            WatchPartnerThinking(talker);

            inputField.Focus();
            Log.Info($"Talk window opened with {partnerNameLabel.text}");
        }

        private void CloseTalk()
        {
            if (shownContact == null) return;

            StopWatchingPartnerThinking();
            StopAllCoroutines();
            shownContact = null;
            talkingOverRadio = false;
            drawnLineCount = 0;
            linesScrollView.Clear();
            windowElement.style.display = DisplayStyle.None;
            ShowPartnerThinking(false);
            inputField.value = string.Empty;

            Log.Info("Talk window closed");
        }

        private void RedrawAllLines()
        {
            StopAllCoroutines();
            linesScrollView.Clear();
            drawnLineCount = 0;
            DrawNewLines(false);
        }

        private void HandleLinesArrived(long partnerId)
        {
            if (shownContact == null || partnerId != shownContact.PartnerId) return;

            DrawNewLines(true);
        }

        private void HandleContactsCleared()
        {
            CloseTalk();
        }

        // Stops at the first gap, so a later line never shows above one that is still on its way
        private void DrawNewLines(bool typeOut)
        {
            while (drawnLineCount < shownContact.LineCount && shownContact.TryGetLine(drawnLineCount, out ConversationLine line))
            {
                AddLine(line, typeOut);
                drawnLineCount++;
            }

            shownContact.SeenLineCount = shownContact.LineCount;
        }

        private void AddLine(ConversationLine line, bool typeOut)
        {
            bool isOwnLine = line.AuthorId == playerConversations.CharacterId;

            Label lineLabel = new();
            lineLabel.AddToClassList("talk__line");
            if (isOwnLine) lineLabel.AddToClassList("talk__line--mine");
            if (!line.Spoken) lineLabel.AddToClassList("talk__line--radio");

            linesScrollView.Add(lineLabel);

            if (isOwnLine || !typeOut)
            {
                lineLabel.text = line.Content;
            }
            else
            {
                StartCoroutine(TypeOutLine(lineLabel, line.Content));
            }

            linesScrollView.schedule.Execute(() =>
            {
                if (lineLabel.parent != null) linesScrollView.ScrollTo(lineLabel);
            });
        }

        private IEnumerator TypeOutLine(Label lineLabel, string content)
        {
            for (int i = 1; i <= content.Length; i++)
            {
                lineLabel.text = content.Substring(0, i);
                linesScrollView.ScrollTo(lineLabel);
                yield return new WaitForSeconds(charInterval);
            }
        }

        private void WatchPartnerThinking(Talker talker)
        {
            if (talker == null)
            {
                ShowPartnerThinking(false);
                return;
            }

            partnerTalker = talker;
            partnerTalker.ThinkingChanged += ShowPartnerThinking;
            ShowPartnerThinking(partnerTalker.IsThinking);
        }

        private void StopWatchingPartnerThinking()
        {
            if (partnerTalker == null) return;

            partnerTalker.ThinkingChanged -= ShowPartnerThinking;
            partnerTalker = null;
        }

        private void ShowPartnerThinking(bool thinking)
        {
            waitingLabel.style.display = thinking ? DisplayStyle.Flex : DisplayStyle.None;
            inputField.SetEnabled(!thinking);
            if (!thinking && windowElement.style.display == DisplayStyle.Flex) inputField.Focus();
        }

        private void HandleInputKeyDown(KeyDownEvent keyDown)
        {
            if (keyDown.keyCode != KeyCode.Return && keyDown.keyCode != KeyCode.KeypadEnter) return;

            string speech = inputField.value.Trim();
            inputField.value = string.Empty;
            keyDown.StopPropagation();

            if (speech.Length == 0 || shownContact == null) return;

            if (talkingOverRadio && playerRadio != null)
            {
                playerRadio.SayRpc(shownContact.PartnerId, speech);
            }
            else
            {
                playerMouth.SayRpc(speech);
            }
        }

        private void OpenInventoryFromTalk()
        {
            LocalPlayer localPlayer = OwnPlayer.FindComponent<LocalPlayer>();
            if (localPlayer != null) localPlayer.OpenInventoryFromTalk();
        }

        private static Talker FindTalkerByNetworkObjectId(ulong networkObjectId)
        {
            NetworkManager network = NetworkManager.Singleton;
            if (network == null || network.SpawnManager == null) return null;

            return network.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject networkObject)
                ? networkObject.GetComponentInChildren<Talker>()
                : null;
        }

        private string GetPartnerName(Component partner)
        {
            Nameable nameable = partner == null ? null : partner.GetComponentInChildren<Nameable>();
            if (nameable == null) return Stranger;

            string shownName = nameable.GetShownName();

            return string.IsNullOrEmpty(shownName) ? Stranger : shownName;
        }

        private void CloseTalkAndUnsubscribeFromOwnPlayer()
        {
            CloseTalk();

            if (playerConversations != null)
            {
                playerConversations.LinesArrived -= HandleLinesArrived;
                playerConversations.ContactsCleared -= HandleContactsCleared;
                playerConversations = null;
            }

            if (playerMouth == null) return;

            playerMouth.TalkOpened -= OpenTalk;
            playerMouth.TalkClosed -= CloseTalk;
            playerMouth = null;
            playerRadio = null;
        }
    }
}
