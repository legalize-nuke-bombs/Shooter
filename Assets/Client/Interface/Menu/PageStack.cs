using System.Collections.Generic;

namespace Shooter.Client.Interface
{
    public class PageStack
    {
        private readonly List<MenuPage> pages = new();

        public MenuPage Top => pages.Count == 0 ? null : pages[pages.Count - 1];

        public void Push(MenuPage page)
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
