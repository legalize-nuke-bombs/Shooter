using System.IO;
using System.IO.Compression;

namespace Shooter.Game.Core.Saves
{
    public class ZipManager : CompressionManager
    {
        protected override void CompressRaw(string path)
        {
            ZipFile.CreateFromDirectory(path, path + Extension);
        }

        protected override byte[] ReadRaw(string location, string file)
        {
            using ZipArchive zip = ZipFile.OpenRead(location);
            ZipArchiveEntry entry = zip.GetEntry(file);
            if (entry == null) return null;

            using Stream stream = entry.Open();
            using var buffer = new MemoryStream();
            stream.CopyTo(buffer);

            return buffer.ToArray();
        }

        public override string Key => "Zip";
        public override string Extension => ".zip";
    }
}
