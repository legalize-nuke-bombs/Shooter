using System.IO;

namespace Shooter.Infrastructure
{
    public class NoCompressionManager : CompressionManager
    {
        protected override void CompressFolderRaw(string folderPath)
        {
        }

        protected override byte[] ReadFileRaw(string archivePath, string fileName)
        {
            string filePath = Path.Combine(archivePath, fileName);
            return File.Exists(filePath) ? File.ReadAllBytes(filePath) : null;
        }

        protected override void DeleteArchiveRaw(string archivePath)
        {
            Directory.Delete(archivePath, true);
        }

        public override string Key => "";
        public override string Extension => "";
    }
}
