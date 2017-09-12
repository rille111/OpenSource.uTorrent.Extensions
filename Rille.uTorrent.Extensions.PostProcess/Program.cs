using System;
using System.Linq;
using NLog;
using Rille.uTorrent.Extensions.PostProcess.Model;
using Rille.uTorrent.Extensions.PostProcess.Services;
using System.Threading;

namespace Rille.uTorrent.Extensions.PostProcess
{
    class Program
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private static Config _config = Config.Create();
        private static ITorrentManager _torrentManager;
        private static FileManager _fileManager = new FileManager(_config);
        private static Unpacker unpacker = new Unpacker(_config, _fileManager);
        private static int processedTorrentsCount = 0;
        static Mutex mutex = new Mutex(false, "https://github.com/rille111/Rille.uTorrent.Extensions");

        static void Main(string[] args)
        {
            // Wait 5 seconds if contended – in case another instance
            // of the program is in the process of shutting down.
            if (!mutex.WaitOne(TimeSpan.Zero, false))
            {
                _logger.Debug("- Another instance of the app is running. Bye!");
                return;
            }
            try
            {
                var exitCode = Run(args);
                ExitApp(exitCode);
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }

        private static int Run(string[] args)
        {
            try
            {
                _logger.Info("Application starting.");

                ValidateConfig();
                CreateTorrentManager();

                var torrents = _torrentManager.GetTorrentList();

                if (torrents == null || torrents.Count == 0)
                {
                    _logger.Error("No torrents found!");
                    return 666;
                }

                if (_config.RestartErrorTorrents)
                {
                    foreach (var torrent in torrents.Where(p => p.TorrentStatus.HasFlag(TorrentStatus.Error)))
                    {
                        _torrentManager.Start(torrent);
                    }
                }

                _logger.Info($"LOOPING TORRENTS");
                foreach (var torrent in torrents)
                {
                    // TODO: TESTING
                    //if (torrent.Hash != "2D8043305E789EDBBA5537E569D3DF56A6E6E3E8")
                    //  continue;

                    LogStartProcessTorrent(torrent);
                    HandleAlreadyProcessedTorrent(torrent);
                    HandleUnprocessedTorrent(torrent);

                    if (processedTorrentsCount >= _config.MaxProcessTorrentsInBatch)
                    {
                        // Enough in this batch already. Exit.
                        _logger.Debug($"- Already Processed {processedTorrentsCount} in this batch which was the configured max, exiting..");
                        break;
                    }
                }
                _logger.Info($"FINISHED");
            }
            catch (Exception ex)
            {
                _logger.Fatal(ex, "Unexpected error occurred.");
                return 666;
            }
            return 0;
        }

        private static void HandleUnprocessedTorrent(Torrent torrent)
        {
            if (torrent.IsDownloaded && !_torrentManager.HasTorrentBeenProcessed(torrent))
            {
                if (processedTorrentsCount >= _config.MaxProcessTorrentsInBatch)
                {
                    // Enough in this batch already. Exit.
                    _logger.Debug($"- Already Processed {processedTorrentsCount} in this batch which was the configured max, exiting..");
                    return;
                }

                // Otherwise, lets unpack/process!
                processedTorrentsCount++;

                // Mark as in progress
                _torrentManager.MarkTorrentAsProcessing(torrent);

                var unpackedOk = unpacker.CopyAndUnpack(torrent);
                if (unpackedOk)
                {
                    // Mark torrent as finished
                    _torrentManager.MarkTorrentAsProcessed(torrent);
                    _logger.Info($"- Torrent process OK!");

                    if (_torrentManager.HasTorrentBeenProcessed(torrent) && _torrentManager.HasTorrentGoalsBeenReached(torrent))
                    {
                        // Delete if goals reached and torrent processed ok, if configured as such.
                        if (_config.DeleteFromTorrentsFolderWhenUnpacked)
                        {
                            _logger.Info("- Deleting (torrent is processed and goals has been reached.");
                            _torrentManager.DeleteTorrent(torrent);
                        }
                    }
                }
                else
                {
                    // Unpack error!! Quit!
                    _logger.Error($"- Failed to process torrent! Investigate logs.");
                    _torrentManager.MarkTorrentAsProcessFailed(torrent);
                }
            }
        }

        private static void HandleAlreadyProcessedTorrent(Torrent torrent)
        {
            if (_torrentManager.HasTorrentBeenProcessed(torrent))
            {
                if (_torrentManager.HasTorrentGoalsBeenReached(torrent) && _config.DeleteAlreadyProcessedTorrents)
                {
                    // Torrents goal reached, and configured to deleted finished torrents, so delete it.
                    _logger.Info("- Deleting (torrent is processed and goals has been reached.");
                    _torrentManager.DeleteTorrent(torrent);
                }
            }
        }

        private static void LogStartProcessTorrent(Torrent torrent)
        {
            _logger.Info($"Torrent: {torrent.Name} -");
            _logger.Debug($"- Status: {torrent.TorrentStatus}");
            _logger.Debug($"- ProcessingStatus: {torrent.ProcessingStatus}");
            _logger.Debug($"- IsDownloaded: {torrent.IsDownloaded}");
            _logger.Debug($"- HasTorrentGoalsBeenReached: {_torrentManager.HasTorrentGoalsBeenReached(torrent)}");
            _logger.Debug($"- Ratio: {torrent.ActualSeedRatioPercent}");
            _logger.Debug($"- Path: {torrent.Path}");
            _logger.Debug($"- IsFolder: {torrent.IsFolder}");
            _logger.Debug($"- IsSingleFileAndArchive: {torrent.IsSingleFileAndArchive}");
            _logger.Debug($"- IsSingleFileButNotArchive: {torrent.IsSingleFileButNotArchive}");
            _logger.Debug($"- DoesFolderContainAnyArchive: {_fileManager.DoesFolderContainAnyArchive(torrent.Path)}");
            
        }

        private static void CreateTorrentManager()
        {
            if (_config.OperatingMode == OperatingMode.UnpackTorrentsFolderOnly)
                _torrentManager = new FolderBasedTorrentManager(_config, _fileManager);
            else
                _torrentManager = new UTorrentManager(_config);
        }

        private static void ValidateConfig()
        {
            var result = new ConfigValidator().Validate(_config);

            if (result.IsValid)
            {
                _logger.Info("Config is valid.");
            }
            else
            {
                var errors = string.Join("\n", result.Errors.Select(p => p.PropertyName + ": " + p.ErrorMessage + " Value: " + p.AttemptedValue));
                throw new InvalidProgramException("Invalid configuration!\n" + errors);
            }
        }

        private static void ExitApp(int exitCode)
        {
            Console.WriteLine("-- Finished, press any key to exit --");
            //Console.ReadKey();
            Environment.Exit(exitCode);
        }

    }
}
