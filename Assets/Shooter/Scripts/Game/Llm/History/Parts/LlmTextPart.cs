namespace Shooter.Game
{
    public sealed class LlmTextPart : LlmPart
    {
        public LlmTextPart(string text)
        {
            Text = text;
        }

        public string Text { get; set; }

        public override int Length => Text.Length;
    }
}
