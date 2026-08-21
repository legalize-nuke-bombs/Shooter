using System;
using System.IO;
using System.IO.Compression;
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

        private SaveEntry(string location, bool compressed, Meta meta)
        {
            Location = location;
            Compressed = compressed;
            Meta = meta;
        }

        public string Location { get; }

        public bool Compressed { get; }

        public Meta Meta { get; }

        public bool Foreign => Meta.Version != Application.version;

        public static SaveEntry Read(string location, bool compressed)
        {
            byte[] bytes = ReadFile(location, compressed, MetaFile);
            if (bytes == null)
            {
                Log.Warn($"Save {location} has no {MetaFile}, skipped");
                return null;
            }

            try
            {
                using var reader = new StreamReader(new MemoryStream(bytes), Encoding.UTF8, true);
                Meta meta = JsonConvert.DeserializeObject<Meta>(reader.ReadToEnd(), Meta.Json);

                return new SaveEntry(location, compressed, meta);
            }
            catch (Exception e)
            {
                Log.Warn($"Save {location} has unreadable {MetaFile}, skipped: {e.Message}");
                return null;
            }
        }

        public byte[] ReadPreview()
        {
            return ReadFile(Location, Compressed, PreviewFile);
        }

        private static byte[] ReadFile(string location, bool compressed, string file)
        {
            try
            {
                if (!compressed)
                {
                    string path = Path.Combine(location, file);
                    return File.Exists(path) ? File.ReadAllBytes(path) : null;
                }

                using ZipArchive zip = ZipFile.OpenRead(location);
                ZipArchiveEntry entry = zip.GetEntry(file);
                if (entry == null) return null;

                using Stream stream = entry.Open();
                using var buffer = new MemoryStream();
                stream.CopyTo(buffer);

                return buffer.ToArray();
            }
            catch (Exception e)
            {
                Log.Warn($"Save {location}: {file} can not be read: {e.Message}");
                return null;
            }
        }
    }
}
