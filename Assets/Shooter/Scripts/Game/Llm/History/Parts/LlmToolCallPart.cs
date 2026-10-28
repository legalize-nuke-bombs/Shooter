namespace Shooter.Game
{
    public sealed class LlmToolCallPart : LlmPart
    {
        public LlmToolCallPart(string id, string name, string arguments)
        {
            Id = id;
            Name = name;
            Arguments = arguments;
        }

        public string Id { get; }
        public string Name { get; }
        public string Arguments { get; }

        public override int Length => Name.Length + Arguments.Length;
    }
}
