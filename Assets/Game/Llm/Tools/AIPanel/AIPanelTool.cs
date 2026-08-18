using System;
using System.Collections.Generic;
using System.Text;
using Shooter.Game.AI;
using Shooter.Game.Core;
using Shooter.Logging;

namespace Shooter.Game.Llm.AIPanel
{
    public class AIPanelTool : LlmTool<AIPanelArguments>
    {
        private static readonly Journal Log = Logs.Here();

        private AISetting[] settings;

        public override string Name => "ai_panel";

        public override string Description =>
            @"
Use this tool to customize your AI.
To get a list of available parameters and their current values, call this tool with an empty dict of overrides.
";

        protected override void Awake()
        {
            base.Awake();
            settings = GetComponents<AISetting>();
            Log.Info($"Entity {name} has {settings.Length} ai settings");
        }

        protected override string Execute(AIPanelArguments arguments)
        {
            if (arguments.Overrides == null || arguments.Overrides.Count == 0) return RepresentSettings();
            return Override(arguments.Overrides);
        }

        private string RepresentSettings()
        {
            var sb = new StringBuilder();

            foreach (AISetting setting in settings)
            {
                sb.AppendLine(setting.Name + " (" + setting.Range + "): " + setting.Get());
                sb.AppendLine(setting.Description);
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private string Override(Dictionary<string, string> overrides)
        {
            var sb = new StringBuilder();

            var found = new HashSet<string>();
            foreach (AISetting setting in settings)
                if (overrides.TryGetValue(setting.Name, out string value))
                {
                    found.Add(setting.Name);
                    try
                    {
                        setting.Set(value);
                        sb.AppendLine($"Successfully updated {setting.Name} to {value}");
                    }
                    catch (Exception e)
                    {
                        sb.AppendLine($"Failed to update {setting.Name} to {value}: {e.Message}");
                    }
                }

            foreach (string key in overrides.Keys)
                if (!found.Contains(key))
                    sb.AppendLine($"Failed to find parameter {key}");

            return sb.ToString();
        }
    }
}
