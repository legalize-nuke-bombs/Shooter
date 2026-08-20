using System.IO.Compression;

namespace Shooter.Game.Core.Saves
{
    public class ZipManager : CompressionManager
    {
        protected override void CompressRaw(string path)
        {
            ZipFile.CreateFromDirectory(path, path + Extension);
        }

        public override string Key => "Zip";
        public override string Extension => ".zip";
    }
}
