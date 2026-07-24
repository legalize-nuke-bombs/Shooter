namespace Shooter.Server.Worlds.Entities.Parts.Talker.AITalker.Gemini
{
    public enum GeminiModel
    {
        Flash35,
        Flash35Lite
    }

    public static class GeminiModelExtensions
    {
        public static string ToRaw(this GeminiModel model)
        {
            return model switch
            {
                GeminiModel.Flash35 => "gemini-3.5-flash",
                GeminiModel.Flash35Lite => "gemini-3.5-flash-lite",
                _ => "gemini-3.5-flash"
            };
        }
    }
}
