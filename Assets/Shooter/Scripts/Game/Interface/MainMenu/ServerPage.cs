using System;
using System.Collections.Generic;
using System.Linq;
using Shooter.Infrastructure;
using UnityEngine.UIElements;

namespace Shooter.Game
{
    public abstract class ServerPage : MainMenuPage
    {
        private const string CompressionField = "compression";
        private const string ProviderField = "provider";
        private const string NoProvider = "";
        private static readonly Journal Log = Logs.CreateJournalForThisFile();

        protected ServerPage(VisualElement root) : base(root)
        {
            Offer(Require<DropdownField>(CompressionField), MainCompressionManager.Keys, Titles.Compression);

            IEnumerable<string> providers = new[] { NoProvider }.Concat(OpenAiHosts.ProviderNames);
            Offer(Require<DropdownField>(ProviderField), providers, Titles.Provider);
        }

        protected override void Closed()
        {
            Config.SaveGameConfig();

            ServerConfig server = Config.GetGameConfig().Server;
            Log.Info(
                $"Server settings: port {server.Port}, saves as '{server.SaveCompressionAlgorithm}', model {server.Llm.Provider}/{server.Llm.Model}");
        }
    }
}
