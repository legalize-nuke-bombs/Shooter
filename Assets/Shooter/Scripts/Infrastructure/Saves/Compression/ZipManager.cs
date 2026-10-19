using System.IO;
using System.IO.Compression;

namespace Shooter.Infrastructure
{
    public class ZipManager : CompressionManager
    {
        protected override void CompressFolderRaw(string folderPath)
        {
            ZipFile.CreateFromDirectory(folderPath, folderPath + Extension);
        }

        protected override byte[] ReadFileRaw(string archivePath, string fileName)
        {
            using ZipArchive archive = ZipFile.OpenRead(archivePath);
            ZipArchiveEntry entry = archive.GetEntry(fileName);
            if (entry == null) return null;

            using Stream stream = entry.Open();
            using MemoryStream buffer = new();
            stream.CopyTo(buffer);

            return buffer.ToArray();
        }

        public override string Key => "Zip";
        public override string Extension => ".zip";
    }
}
