using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

namespace Shooter.Infrastructure
{
    public sealed class SaveEntry
    {
        private const string MetaFileName = "Meta.json";
        private const string PreviewFileName = "Preview.jpg";
        private static readonly Journal Log = Logs.Here();

        private SaveEntry(string location, Meta meta)
        {
            Location = location;
            Meta = meta;
        }

        public string Location { get; }

        public Meta Meta { get; }

        public bool IsFromAnotherVersion => Meta.Version != Application.version;

        public static SaveEntry ReadFromLocation(string location)
        {
            byte[] metaBytes = MainCompressionManager.ReadFile(location, MetaFileName);
            if (metaBytes == null)
            {
                Log.Warn($"Save {location} has no {MetaFileName}, skipped");
                return null;
            }

            try
            {
                using StreamReader reader = new(new MemoryStream(metaBytes), Encoding.UTF8, true);
                Meta meta = JsonConvert.DeserializeObject<Meta>(reader.ReadToEnd(), Meta.JsonSettings);

                return new SaveEntry(location, meta);
            }
            catch (Exception e)
            {
                Log.Warn($"Save {location} has unreadable {MetaFileName}, skipped: {e.Message}");
                return null;
            }
        }

        public byte[] ReadPreview()
        {
            return MainCompressionManager.ReadFile(Location, PreviewFileName);
        }
    }
}
