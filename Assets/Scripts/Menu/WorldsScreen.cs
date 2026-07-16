using System;
using UnityEngine;
using UnityEngine.UIElements;
using Shooter.Shared;

namespace Shooter.Menu
{
    public class WorldsScreen
    {
        private const int PageSize = 20;

        private readonly VisualElement screen;
        private readonly ScrollView scroll;
        private readonly Button tabPublic;
        private readonly Button tabMine;
        private readonly Button loadMoreBtn;
        private readonly TextField worldIdField;
        private readonly Label status;
        private readonly Label userLabel;

        private readonly MenuApi api;
        private readonly Action onCreateClick;
        private readonly Action onJoined;

        private bool mineTab = true;
        private bool busy;
        private int page;

        public WorldsScreen(VisualElement root, MenuApi api, Action onCreateClick, Action onJoined)
        {
            this.api = api;
            this.onCreateClick = onCreateClick;
            this.onJoined = onJoined;

            screen = root.Q<VisualElement>("worlds-screen");
            scroll = root.Q<ScrollView>("worlds-scroll");
            tabPublic = root.Q<Button>("tab-public");
            tabMine = root.Q<Button>("tab-mine");
            loadMoreBtn = root.Q<Button>("load-more-btn");
            worldIdField = root.Q<TextField>("world-id-field");
            status = root.Q<Label>("worlds-status");
            userLabel = root.Q<Label>("user-label");

            root.Q<Button>("refresh-btn").clicked += Reload;
            root.Q<Button>("create-btn").clicked += () => onCreateClick();
            root.Q<Button>("join-id-btn").clicked += () => { string id = worldIdField.value.Trim(); if (id.Length > 0) Join(id); };
            loadMoreBtn.clicked += () => { page++; LoadWorlds(false); };
            tabPublic.clicked += () => SwitchTab(false);
            tabMine.clicked += () => SwitchTab(true);
        }

        public void Show()
        {
            userLabel.text = Session.DisplayName;
            screen.RemoveFromClassList("hidden");
            Reload();
        }

        public void Hide() => screen.AddToClassList("hidden");

        public void ShowMineAndReload()
        {
            if (!mineTab) SwitchTab(true);
            else Reload();
        }

        private void SwitchTab(bool mine)
        {
            mineTab = mine;
            if (mine) { tabMine.AddToClassList("list-tab-active"); tabPublic.RemoveFromClassList("list-tab-active"); }
            else { tabPublic.AddToClassList("list-tab-active"); tabMine.RemoveFromClassList("list-tab-active"); }
            Reload();
        }

        private void Reload()
        {
            page = 0;
            LoadWorlds(true);
        }

        private void LoadWorlds(bool reset)
        {
            if (reset) scroll.Clear();
            status.text = "";

            int requestedPage = page;
            api.LoadWorlds(requestedPage, PageSize, mineTab, (worlds, error) =>
            {
                if (error != null) { status.text = error; return; }

                foreach (WorldDto world in worlds)
                    scroll.Add(BuildCard(world));

                loadMoreBtn.style.display = worlds.Length < PageSize ? DisplayStyle.None : DisplayStyle.Flex;

                if (worlds.Length == 0 && requestedPage == 0)
                {
                    var empty = new Label(mineTab ? "Пока пусто — создай свой первый мир" : "Публичных миров пока нет");
                    empty.AddToClassList("empty-note");
                    scroll.Add(empty);
                }
            });
        }

        private VisualElement BuildCard(WorldDto world)
        {
            var card = new VisualElement();
            card.AddToClassList("world-card");
            if (world.visibilityPolicy == "PRIVATE")
                card.AddToClassList("world-card-private");

            var info = new VisualElement();
            info.AddToClassList("world-info");

            var nameRow = new VisualElement();
            nameRow.AddToClassList("world-name-row");
            var name = new Label(world.name);
            name.AddToClassList("world-name");
            nameRow.Add(name);

            if (world.visibilityPolicy == "PRIVATE")
                nameRow.Add(MakeBadge("СКРЫТЫЙ", "badge-private"));

            string myRole = FindMyRole(world);
            if (myRole == "CREATOR") nameRow.Add(MakeBadge("СОЗДАТЕЛЬ", "badge-creator"));
            else if (myRole == "MODERATOR") nameRow.Add(MakeBadge("МОДЕРАТОР", "badge-moderator"));
            else if (myRole == "MEMBER") nameRow.Add(MakeBadge("УЧАСТНИК", "badge-member"));

            info.Add(nameRow);

            if (!string.IsNullOrEmpty(world.description))
            {
                string desc = world.description.Length > 120 ? world.description.Substring(0, 120) + "…" : world.description;
                var descLabel = new Label(desc);
                descLabel.AddToClassList("world-desc");
                info.Add(descLabel);
            }

            var meta = new Label(BuildMeta(world));
            meta.AddToClassList("world-meta");
            info.Add(meta);

            if (world.players != null && world.players.Length > 0)
            {
                var playersRow = new VisualElement();
                playersRow.AddToClassList("players-row");
                var sorted = (PlayerDto[])world.players.Clone();
                Array.Sort(sorted, (a, b) => RoleRank(a.role) != RoleRank(b.role)
                    ? RoleRank(a.role) - RoleRank(b.role)
                    : a.memberSince.CompareTo(b.memberSince));
                foreach (PlayerDto p in sorted)
                {
                    string chipName = p.user != null ? p.user.displayName : "игрок " + p.id;
                    var chip = new Label(p.role == "CREATOR" ? "★ " + chipName : chipName);
                    chip.AddToClassList("player-chip");
                    chip.AddToClassList("player-chip-" + (p.role ?? "MEMBER").ToLowerInvariant());
                    playersRow.Add(chip);
                }
                info.Add(playersRow);
            }

            card.Add(info);

            var joinBtn = new Button(() => Join(world.id)) { text = "ИГРАТЬ" };
            joinBtn.AddToClassList("btn");
            joinBtn.AddToClassList("px");
            joinBtn.AddToClassList("play-btn");
            card.Add(joinBtn);

            return card;
        }

        private static Label MakeBadge(string text, string styleClass)
        {
            var badge = new Label(text);
            badge.AddToClassList("badge");
            badge.AddToClassList("px");
            badge.AddToClassList(styleClass);
            return badge;
        }

        private static string FindMyRole(WorldDto world)
        {
            if (world.players == null || Session.UserId < 0) return null;
            foreach (PlayerDto p in world.players)
                if (p.user != null && p.user.id == Session.UserId)
                    return p.role;
            return null;
        }

        private static string BuildMeta(WorldDto world)
        {
            int count = world.players?.Length ?? 0;
            long age = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - world.createdAt;
            string ago = age < 3600 ? Math.Max(1, age / 60) + " мин назад" : age < 86400 ? (age / 3600) + " ч назад" : (age / 86400) + " дн назад";
            string players = count + (count % 10 == 1 && count % 100 != 11 ? " игрок" : (count % 10 >= 2 && count % 10 <= 4 && (count % 100 < 12 || count % 100 > 14) ? " игрока" : " игроков"));
            return players + " · " + ago;
        }

        private static int RoleRank(string role)
        {
            switch (role)
            {
                case "CREATOR": return 0;
                case "MODERATOR": return 1;
                default: return 2;
            }
        }

        private void Join(string worldId)
        {
            if (busy) return;
            busy = true;
            status.text = "";

            api.Join(worldId, (worldToken, error) =>
            {
                busy = false;
                if (error != null) { status.text = error; return; }

                Session.WorldToken = worldToken;
                onJoined();
            });
        }
    }
}
