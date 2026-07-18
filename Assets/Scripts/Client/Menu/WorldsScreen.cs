using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using Shooter.Client.Account;

namespace Shooter.Client.Menu
{
    public class WorldsScreen
    {
        private const int PageSize = 20;

        private readonly VisualElement screen;
        private readonly ScrollView scroll;
        private readonly Button loadMoreBtn;
        private readonly TextField worldIdField;
        private readonly Label status;
        private readonly Label userLabel;

        private readonly MenuApi api;
        private readonly ErrorModal errors;
        private readonly Action onCreateClick;
        private readonly Action onJoined;

        private bool busy;
        private int page;

        public WorldsScreen(VisualElement root, MenuApi api, ErrorModal errors, Action onCreateClick, Action onJoined)
        {
            this.api = api;
            this.errors = errors;
            this.onCreateClick = onCreateClick;
            this.onJoined = onJoined;

            screen = root.Q<VisualElement>("worlds-screen");
            scroll = root.Q<ScrollView>("worlds-scroll");
            loadMoreBtn = root.Q<Button>("load-more-btn");
            worldIdField = root.Q<TextField>("world-id-field");
            status = root.Q<Label>("worlds-status");
            userLabel = root.Q<Label>("user-label");

            root.Q<Button>("refresh-btn").clicked += Reload;
            root.Q<Button>("create-btn").clicked += () => onCreateClick();
            root.Q<Button>("join-id-btn").clicked += JoinById;
            worldIdField.RegisterCallback<KeyDownEvent>(e => { if (e.keyCode == UnityEngine.KeyCode.Return) JoinById(); });
            loadMoreBtn.clicked += () => { page++; LoadWorlds(false); };
        }

        public void Show()
        {
            userLabel.text = Session.DisplayName;
            screen.RemoveFromClassList("hidden");
            Reload();
        }

        public void Hide() => screen.AddToClassList("hidden");

        public void Reload()
        {
            page = 0;
            LoadWorlds(true);
        }

        private void LoadWorlds(bool reset)
        {
            if (reset) scroll.Clear();
            status.text = "";

            int requestedPage = page;
            api.LoadWorlds(requestedPage, PageSize, (worlds, error) =>
            {
                if (error != null) { status.text = error; return; }

                foreach (WorldDto world in worlds)
                    scroll.Add(BuildSlot(world));

                loadMoreBtn.style.display = worlds.Count < PageSize ? DisplayStyle.None : DisplayStyle.Flex;

                if (worlds.Count == 0 && requestedPage == 0)
                {
                    var empty = new Label("Сохранённых миров нет.\nСоздайте новый мир или войдите по идентификатору.");
                    empty.AddToClassList("empty-note");
                    scroll.Add(empty);
                }
            });
        }

        private VisualElement BuildSlot(WorldDto world)
        {
            var slot = new VisualElement();
            slot.AddToClassList("world-slot");

            var info = new VisualElement();
            info.AddToClassList("world-info");

            var nameRow = new VisualElement();
            nameRow.AddToClassList("world-name-row");
            var name = new Label(world.Name);
            name.AddToClassList("world-name");
            nameRow.Add(name);

            if (FindMyRole(world) == "CREATOR")
                nameRow.Add(MakeBadge("ВЛАДЕЛЕЦ"));
            if (world.JoinPolicy == "NOBODY")
                nameRow.Add(MakeBadge("ЗАКРЫТ ДЛЯ ВХОДА"));

            info.Add(nameRow);

            var meta = new Label(BuildMeta(world));
            meta.AddToClassList("world-meta");
            info.Add(meta);

            if (world.Players != null && world.Players.Count > 0)
            {
                var playersRow = new VisualElement();
                playersRow.AddToClassList("players-row");
                var sorted = new List<PlayerDto>(world.Players);
                sorted.Sort((a, b) => RoleRank(a.Role) != RoleRank(b.Role)
                    ? RoleRank(a.Role) - RoleRank(b.Role)
                    : a.MemberSince.CompareTo(b.MemberSince));
                foreach (PlayerDto player in sorted)
                {
                    string chipName = player.User != null ? player.User.DisplayName : "игрок " + player.Id;
                    var chip = new Label(player.Role == "CREATOR" ? "★ " + chipName : chipName);
                    chip.AddToClassList("player-chip");
                    if (player.Role == "CREATOR") chip.AddToClassList("player-chip-creator");
                    playersRow.Add(chip);
                }
                info.Add(playersRow);
            }

            slot.Add(info);

            var joinBtn = new Button(() => Join(world.Id)) { text = "ВОЙТИ" };
            joinBtn.AddToClassList("btn");
            joinBtn.AddToClassList("enter-btn");
            slot.Add(joinBtn);

            return slot;
        }

        private static Label MakeBadge(string text)
        {
            var badge = new Label(text);
            badge.AddToClassList("badge");
            return badge;
        }

        private static string FindMyRole(WorldDto world)
        {
            if (world.Players == null || Session.UserId < 0) return null;
            foreach (PlayerDto player in world.Players)
                if (player.User != null && player.User.Id == Session.UserId)
                    return player.Role;
            return null;
        }

        private static string BuildMeta(WorldDto world)
        {
            int count = world.Players?.Count ?? 0;
            long age = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - world.CreatedAt;
            string ago = age < 3600 ? Math.Max(1, age / 60) + " мин назад" : age < 86400 ? (age / 3600) + " ч назад" : (age / 86400) + " дн назад";
            string members = count + (count % 10 == 1 && count % 100 != 11 ? " участник" : (count % 10 >= 2 && count % 10 <= 4 && (count % 100 < 12 || count % 100 > 14) ? " участника" : " участников"));
            return members + " · создан " + ago;
        }

        private static int RoleRank(string role) => role == "CREATOR" ? 0 : 1;

        private void JoinById()
        {
            string id = worldIdField.value.Trim();
            if (id.Length == 0) { errors.Show("Укажите идентификатор мира."); return; }
            Join(id);
        }

        private void Join(string worldId)
        {
            if (busy) return;
            busy = true;

            api.Join(worldId, error =>
            {
                busy = false;
                if (error != null) { errors.Show(error); return; }

                onJoined();
            });
        }
    }
}
