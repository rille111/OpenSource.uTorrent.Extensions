using System.Collections.Generic;
using Rille.uTorrent.Extensions.PostProcess.Model;

namespace Rille.uTorrent.Extensions.PostProcess.Services
{
    public interface ITorrentManager
    {
        void DeleteTorrent(Torrent torrent);
        List<Torrent> GetTorrentList();
        bool TorrentHasBeenPostProcessed(Torrent torrent);
        bool TorrentGoalsReached(Torrent torrent);

    }
}