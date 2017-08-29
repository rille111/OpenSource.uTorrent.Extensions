using System;
using System.Collections.Generic;
using Rille.uTorrent.Extensions.PostProcess.Model;
using System.IO;
using NLog;

namespace Rille.uTorrent.Extensions.PostProcess.Services
{
    public class FolderBasedTorrentManager : ITorrentManager
    {
        private readonly Config _config;
        private readonly FileManager _fileManager;
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public FolderBasedTorrentManager(Config config, FileManager fileManager)
        {
            _config = config;
            _fileManager = fileManager;
        }

        public void DeleteTorrent(Torrent torrent)
        {
            _logger.Info("- Deleting: " + torrent.Path);

            if (torrent.IsFolder)
            {
                var targetDir = new DirectoryInfo(torrent.Path);
                targetDir.Delete(true);
            }
            else
            {
                var targetFile = new FileInfo(torrent.Path);
                targetFile.Delete();
            }
        }

        public List<Torrent> GetTorrentList()
        {
            var torrents = new List<Torrent>();

            var torrentFolder = new DirectoryInfo(_config.DownloadedTorrentsFolder);
            if (!torrentFolder.Exists)
                throw new DirectoryNotFoundException(_config.DownloadedTorrentsFolder);

            foreach (var item in torrentFolder.EnumerateDirectories())
            {
                torrents.Add(new Torrent(item.Name, _config)
                {
                    Path = item.FullName,
                    Name = item.Name
                });
            }

            foreach (var item in torrentFolder.EnumerateFiles())
            {
                var torr = new Torrent(item.Name, _config)
                {
                    Path = item.FullName,
                    Name = item.Name.Replace(item.Extension, "")
                };
                torrents.Add(torr);
            }

            return torrents;
        }

        public bool HasTorrentBeenPostProcessed(Torrent torrent)
        {
            var targetDir = new DirectoryInfo(torrent.DestinationFolder);
            return targetDir.Exists;
        }

        public bool HasTorrentGoalsBeenReached(Torrent torrent)
        {
            return true;
        }
    }
}
