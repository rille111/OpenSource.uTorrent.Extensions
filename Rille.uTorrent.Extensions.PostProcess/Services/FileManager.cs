using Rille.uTorrent.Extensions.PostProcess.Model;

namespace Rille.uTorrent.Extensions.PostProcess.Services
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

        public bool TorrentGoalsReached(Torrent torrent)
        {
            throw new System.NotImplementedException();
        }
    }
}