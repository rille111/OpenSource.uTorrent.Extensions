using System.Collections.Generic;
using Rille.uTorrent.Extensions.PostProcess.Model;

namespace Rille.uTorrent.Extensions.PostProcess.Services
{
    public interface ITorrentManager
    {
        void DeleteTorrent(Torrent torrent);
        List<Torrent> GetTorrentList();
        bool HasTorrentBeenPostProcessed(Torrent torrent);
        bool HasTorrentGoalsBeenReached(Torrent torrent);
        void MarkTorrentAsProcessing(Torrent torrent);
        void MarkTorrentAsProcessFinished(Torrent torrent);
        void MarkTorrentAsProcessFailed(Torrent torrent);
    }
}