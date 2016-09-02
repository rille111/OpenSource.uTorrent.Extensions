namespace Rille.uTorrent.Extensions.Unpack
{
    public class FileManager
    {
        private readonly Config _config;

        public FileManager(Config config)
        {
            _config = config;
        }

        public bool TorrentHasBeenPostProcessed(Torrent torrent)
        {
            return false;
        }
    }
}