using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Shooter.Client.Account;
using Shooter.Client.Ui;
using Shooter.Client.Worlds;
using Shooter.Client.Worlds.Entities;
using Shooter.Client.Worlds.Entities.Players;
using Shooter.Logging;
using Shooter.Server.Worlds.Entities;
using Shooter.Server.Worlds.Entities.Parts.Pilot;
using Shooter.Server.Worlds.Entities.Parts.Talker;

namespace Shooter.Client.Hud.Talking
{
    public class TalkDialog : UiElement
    {
        private static readonly Color FrameColor = new Color(0.02f, 0.03f, 0.05f, 0.92f);
        private static readonly Color MyColor = new Color(0.85f, 0.62f, 0.45f);
        private static readonly Color MutedColor = new Color(0.45f, 0.48f, 0.53f);

        private readonly Font font;
        private readonly ClientWorld world;
        private readonly PlayerRig rig;
        private readonly TalkSense talkSense;
        private readonly VisualElement frame = new VisualElement();
        private readonly TextLine title;
        private readonly ScrollView history = new ScrollView(ScrollViewMode.Vertical);
        private readonly TextField input = new TextField();

        public bool IsOpen { get; private set; }

        private Guid targetId;
        private int renderedMessages = -1;
        private bool renderedWaiting;
        private bool scrollPending;

        public TalkDialog(Font font, ClientWorld world, PlayerRig rig, TalkSense talkSense)
        {
            this.font = font;
            this.world = world;
            this.rig = rig;
            this.talkSense = talkSense;

            Fullscreen();
            Visible = false;

            frame.style.position = Position.Absolute;
            frame.style.left = Length.Percent(30);
            frame.style.top = Length.Percent(18);
            frame.style.width = Length.Percent(40);
            frame.style.height = Length.Percent(56);
            frame.style.paddingLeft = 16;
            frame.style.paddingRight = 16;
            frame.style.paddingTop = 12;
            frame.style.paddingBottom = 12;
            frame.style.backgroundColor = FrameColor;
            frame.style.flexDirection = FlexDirection.Column;
            Add(frame);

            title = new TextLine(font, 16);
            title.style.marginBottom = 8;
            frame.Add(title);

            history.style.flexGrow = 1;
            history.style.marginBottom = 8;
            frame.Add(history);

            input.maxLength = Talker.SpeechLimit;
            input.style.fontSize = 14;
            input.style.unityFontDefinition = new StyleFontDefinition(FontDefinition.FromFont(font));
            input.RegisterCallback<KeyDownEvent>(OnInputKeyDown);
            frame.Add(input);
        }

        public void Show(EntityView talker)
        {
            IsOpen = true;
            targetId = talker.State.Id;
            title.text = talker.Name;
            renderedMessages = -1;
            input.value = "";
            Visible = true;

            rig.UiCaptured = true;
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
            input.Focus();

            Log.Info("Talk dialog opened for entity {} '{}'", targetId, talker.Name);
        }

        public bool Hide()
        {
            if (!IsOpen)
            {
                return false;
            }

            IsOpen = false;
            Visible = false;
            rig.UiCaptured = false;
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;
            Log.Info("Talk dialog closed");
            return true;
        }

        protected override void OnTick(float dt)
        {
            if (!IsOpen)
            {
                return;
            }

            EntityView talker = talkSense.TargetTalker();
            if (talker == null || talker.State.Id != targetId)
            {
                Hide();
                return;
            }

            List<Message> messages = MyConversation(talker.State)?.Messages;
            int count = messages?.Count ?? 0;
            bool waiting = count > 0 && messages[count - 1].Author == MessageAuthor.Player;

            input.SetEnabled(!waiting);

            if (count != renderedMessages || waiting != renderedWaiting)
            {
                Render(messages, waiting);
                renderedMessages = count;
                renderedWaiting = waiting;
                if (!waiting) input.Focus();
            }

            if (scrollPending)
            {
                history.scrollOffset = new Vector2(0f, history.contentContainer.worldBound.height);
                scrollPending = false;
            }
        }

        private ConversationState MyConversation(EntityState target)
        {
            PilotState pilot = world.Me?.Part<PilotState>();
            if (pilot == null)
            {
                return null;
            }

            TalkerState talker = target.Part<TalkerState>();
            if (talker?.Conversations == null)
            {
                return null;
            }

            return talker.Conversations.TryGetValue(pilot.UserId, out ConversationState conversation) ? conversation : null;
        }

        private void Render(List<Message> messages, bool waiting)
        {
            history.Clear();

            if (messages == null || messages.Count == 0)
            {
                history.Add(Line("Разговор не начат.", MutedColor));
            }
            else
            {
                foreach (Message message in messages)
                {
                    bool mine = message.Author == MessageAuthor.Player;
                    string author = mine ? Session.DisplayName : title.text;
                    history.Add(Line(author + ": " + message.Content, mine ? MyColor : (Color?)null));
                }
            }

            if (waiting)
            {
                history.Add(Line("…", MutedColor));
            }

            scrollPending = true;
        }

        private TextLine Line(string text, Color? color)
        {
            var line = new TextLine(font, 14);
            line.text = text;
            line.style.whiteSpace = WhiteSpace.Normal;
            line.style.marginBottom = 4;
            if (color != null)
            {
                line.style.color = color.Value;
            }

            return line;
        }

        private void OnInputKeyDown(KeyDownEvent e)
        {
            if (e.keyCode != KeyCode.Return && e.keyCode != KeyCode.KeypadEnter)
            {
                return;
            }

            string speech = input.value?.Trim();
            if (string.IsNullOrEmpty(speech))
            {
                return;
            }

            rig.Say(speech);
            input.value = "";
            e.StopPropagation();
        }
    }
}
