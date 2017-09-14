using System.Collections.Generic;
using Rille.uTorrent.Extensions.PostProcess.Model;

namespace Rille.uTorrent.Extensions.PostProcess.Services
{
    public interface ITorrentManager
    {
        void DeleteTorrent(Torrent torrent);
        List<Torrent> GetTorrents(string torrentHash = null);
        void Start(Torrent torrent);
        bool HasTorrentBeenProcessed(Torrent torrent);
        bool HasTorrentGoalsBeenReached(Torrent torrent);
        void MarkTorrentAsProcessing(Torrent torrent);
        void MarkTorrentAsProcessed(Torrent torrent);
        void MarkTorrentAsProcessFailed(Torrent torrent);
    }
}