using System.Collections.Generic;

namespace Shooter.Game
{
    public class PageStack
    {
        private readonly List<MainMenuPage> pages = new();

        public MainMenuPage Top => pages.Count == 0 ? null : pages[pages.Count - 1];

        public void Push(MainMenuPage page)
        {
            Top?.Hide();
            pages.Add(page);
            page.Show();
        }

        public bool Pop()
        {
            if (pages.Count < 2) return false;

            Top.Hide();
            pages.RemoveAt(pages.Count - 1);
            Top.Show();

            return true;
        }

        public void Clear()
        {
            Top?.Hide();
            pages.Clear();
        }
    }
}
