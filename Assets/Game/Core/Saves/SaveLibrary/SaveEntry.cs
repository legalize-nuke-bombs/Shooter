using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Shooter.Logging;
using UnityEngine;

namespace Shooter.Game.Core.Saves
{
    public sealed class SaveEntry
    {
        private const string MetaFile = "Meta.json";
        private const string PreviewFile = "Preview.jpg";
        private static readonly Journal Log = Logs.Here();

        private SaveEntry(string location, Meta meta)
        {
            Location = location;
            Meta = meta;
        }

        public string Location { get; }

        public Meta Meta { get; }

        public bool Foreign => Meta.Version != Application.version;

        public static SaveEntry Read(string location)
        {
            byte[] bytes = MainCompressionManager.Current.Read(location, MetaFile);
            if (bytes == null)
            {
                Log.Warn($"Save {location} has no {MetaFile}, skipped");
                return null;
            }

            try
            {
                using var reader = new StreamReader(new MemoryStream(bytes), Encoding.UTF8, true);
                Meta meta = JsonConvert.DeserializeObject<Meta>(reader.ReadToEnd(), Meta.Json);

                return new SaveEntry(location, meta);
            }
            catch (Exception e)
            {
                Log.Warn($"Save {location} has unreadable {MetaFile}, skipped: {e.Message}");
                return null;
            }
        }

        public byte[] ReadPreview()
        {
            return MainCompressionManager.Current.Read(Location, PreviewFile);
        }
    }
}
