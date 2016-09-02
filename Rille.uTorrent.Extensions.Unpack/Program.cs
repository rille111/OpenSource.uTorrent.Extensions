using System.Linq;
using NLog;

namespace Rille.uTorrent.Extensions.Unpack
{
    class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {
            Logger.Debug("Application starting.");
            var hash = args[0];
            Config config = new Config();
            VerifyConfig(config);
            UTorrentApiManager uTorrentApiManager = new UTorrentApiManager(config);
            FileManager manager = new FileManager(config);

            Logger.Debug($"Torrent hash is: {hash}. I will now load all torrents." );
            var torrents = uTorrentApiManager.GetTorrentList();

            var torrent = torrents.Single(p => p.Hash == hash);
            Logger.Info($"Found torrent. Hash: {torrent.Hash}, Name: {torrent.Name}, NumericStatus: {torrent.NumericStatus}, Status: {torrent.TorrentStatus}");

            if (manager.TorrentHasBeenPostProcessed(torrent))
            {
                uTorrentApiManager.DeleteTorrent(torrent);
            }
            else
            {
                Logger.Warn($"Torrent hash: {hash} has not been post processed yet. (No folder at: {}. Aborting.");
            }
        }

        private static void VerifyConfig(Config config)
        {
            
        }
    }
}
