using System.IO;

namespace Shooter.Game.Core.Saves
{
    public class NoCompressionManager : CompressionManager
    {
        protected override void CompressRaw(string path)
        {
        }

        protected override byte[] ReadRaw(string location, string file)
        {
            string path = Path.Combine(location, file);
            return File.Exists(path) ? File.ReadAllBytes(path) : null;
        }

        protected override void DeleteRaw(string location)
        {
            Directory.Delete(location, true);
        }

        public override string Key => "";
        public override string Extension => "";
    }
}
