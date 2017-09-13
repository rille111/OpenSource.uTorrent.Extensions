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
                _fileManager.DeleteDirectoryRecurse(torrent.Path);
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
                    Name = item.Name,
                    ProgressPercent = 100,
                });
            }

            foreach (var item in torrentFolder.EnumerateFiles())
            {
                var torr = new Torrent(item.Name, _config)
                {
                    //Path = item.FullName,
                    Path = torrentFolder.FullName,
                    FileList = new List<string> { item.Name},
                    Name = item.Name.Replace(item.Extension, ""),
                    ProgressPercent = 100,
                };
                torrents.Add(torr);
            }

            return torrents;
        }

        public bool HasTorrentBeenProcessed(Torrent torrent)
        {
            var targetDir = new DirectoryInfo(torrent.DestinationFolder);
            if (targetDir.Exists)
            {
                if (File.Exists(Path.Combine(targetDir.FullName, "processing.now")))
                {
                    // It never finished, so it hasnt been successfully finished
                    return false;
                }
                else
                {
                    // Destination exists and no in-progress marker file exists, so it is finished!
                    return true;
                }
            }
            else
            {
                // Destination doesnt exist, so it hasnt been processed.
                return false;
            }
        }

        public bool HasTorrentGoalsBeenReached(Torrent torrent)
        {
            return true;
        }

        public void MarkTorrentAsProcessing(Torrent torrent)
        {
            var targetDir = new DirectoryInfo(torrent.DestinationFolder);
            if (!targetDir.Exists)
            {
                targetDir.Create();
            }
            var processingFile = new FileInfo(Path.Combine(targetDir.FullName, "processing.now"));
            if (!processingFile.Exists)
            {
                processingFile.Create().Close();
                processingFile = null; // RELEASE LOCK!!
                // It never finished, so it hasnt been successfully finished
            }
        }

        public void MarkTorrentAsProcessed(Torrent torrent)
        {
            var targetDir = new DirectoryInfo(torrent.DestinationFolder);
            var processingFile = new FileInfo(Path.Combine(targetDir.FullName, "processing.now"));

            if (processingFile.Exists)
            {
                processingFile.Delete();
            }
        }
        
        public void MarkTorrentAsProcessFailed(Torrent torrent)
        {
            var targetDir = new DirectoryInfo(torrent.DestinationFolder);
            if (!targetDir.Exists)
            {
                return;
            }
            // Just leave a remainder file to call for attention ..
            var failedMarkerFile = new FileInfo(Path.Combine(targetDir.FullName, "processing.fail"));
            if (!failedMarkerFile.Exists)
            {
                failedMarkerFile.Create().Close();
            }
        }

        public void Start(Torrent torrent)
        {
            throw new NotImplementedException();
        }
    }
}
