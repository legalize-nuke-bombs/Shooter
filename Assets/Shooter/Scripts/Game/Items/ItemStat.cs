namespace Shooter.Game
{
    // One number of a thing: its name and value as the tooltip shows them to the player, and the same said in English
    // for the residents, who read things through explore_items
    public readonly struct ItemStat
    {
        public ItemStat(string title, string value, string promptDescription)
        {
            Title = title;
            Value = value;
            PromptDescription = promptDescription;
        }

        public string Title { get; }

        public string Value { get; }

        public string PromptDescription { get; }
    }
}
