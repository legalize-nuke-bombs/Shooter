using System.Text;
using Shooter.Game.Body;
using Shooter.Game.World;

namespace Shooter.Game.Llm.ExploreEarSounds
{
    public sealed class ExploreEarSoundsTool : LlmTool<ExploreEarSoundsArguments>
    {
        public override string Name => "explore_ear_sounds";

        public override string Description =>
            "Get a list of the available ear sounds";

        protected override string Execute(ExploreEarSoundsArguments arguments)
        {
            var sb = new StringBuilder();

            EarSoundCatalog catalog = Environment.Current.EarSounds;
            for (int i = 0; i < catalog.Count; i++)
            {
                EarSoundSpec spec = catalog.At(i);
                sb.AppendLine(spec.Id + ": " + spec.PromptDescription);
            }


            return sb.ToString();
        }
    }
}
