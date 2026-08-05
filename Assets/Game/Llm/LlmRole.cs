namespace Shooter.Game.Llm
{
    public enum LlmRole
    {
        User,
        Model,
        System
    }

    public static class LlmRoleExtensions
    {
        public static string Prompt(this LlmRole role)
        {
            switch (role)
            {
                case LlmRole.User:
                    return "Wanderer";
                case LlmRole.Model:
                    return "You";
                case LlmRole.System:
                    return "System";
                default:
                    return "Unknown";
            }
        }
    }
}
