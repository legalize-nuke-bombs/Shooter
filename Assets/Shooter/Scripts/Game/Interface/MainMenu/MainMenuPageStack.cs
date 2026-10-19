using System.Collections.Generic;

namespace Shooter.Game
{
    public class MainMenuPageStack
    {
        private readonly List<MainMenuPage> pages = new();

        public MainMenuPage TopPage => pages.Count == 0 ? null : pages[pages.Count - 1];

        public void PushPage(MainMenuPage page)
        {
            TopPage?.HidePage();
            pages.Add(page);
            page.ShowPage();
        }

        public bool PopPageUnlessLast()
        {
            if (pages.Count < 2) return false;

            TopPage.HidePage();
            pages.RemoveAt(pages.Count - 1);
            TopPage.ShowPage();

            return true;
        }

        public void ClearPages()
        {
            TopPage?.HidePage();
            pages.Clear();
        }
    }
}
