using System;
using System.Collections.Generic;
using System.Linq;
using Shooter.Configuring;
using Shooter.Game.Core.Saves;
using Shooter.Game.Llm;
using Shooter.Logging;
using UnityEngine.UIElements;

namespace Shooter.Client.Interface
{
    public abstract class ServerPage : MenuPage
    {
        private const string CompressionField = "compression";
        private const string BaseProviderField = "base-provider";
        private const string MaxProviderField = "max-provider";
        private const string NoProvider = "";
        private static readonly Journal Log = Logs.Here();

        protected ServerPage(VisualElement root) : base(root)
        {
            Offer(Require<DropdownField>(CompressionField), MainCompressionManager.Current.Keys, Titles.Compression);

            IEnumerable<string> providers = new[] { NoProvider }.Concat(OpenAiHosts.Providers);
            Offer(Require<DropdownField>(BaseProviderField), providers, Titles.Provider);
            Offer(Require<DropdownField>(MaxProviderField), providers, Titles.Provider);
        }

        protected override void Closed()
        {
            Config.Save();

            ServerConfig server = Config.Read().Server;
            Log.Info(
                $"Server settings: port {server.Port}, saves as '{server.SaveCompressionAlgorithm}', models {server.LlmBase.Provider}/{server.LlmBase.Model} and {server.LlmMax.Provider}/{server.LlmMax.Model}");
        }

        private static void Offer(DropdownField field, IEnumerable<string> keys, Func<string, string> title)
        {
            field.choices = keys.ToList();
            field.formatSelectedValueCallback = title;
            field.formatListItemCallback = title;
        }
    }
}
