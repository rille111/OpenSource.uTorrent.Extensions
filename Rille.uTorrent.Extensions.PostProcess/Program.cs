using System;
using System.Linq;
using NLog;
using Rille.uTorrent.Extensions.PostProcess.Model;
using Rille.uTorrent.Extensions.PostProcess.Services;

namespace Rille.uTorrent.Extensions.PostProcess
{
    //TODO: Iterate subfolders and unpack to destination, and then skip those that are archives
    //TODO: Support Ignore file pattern, like .sfv
    class Program
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private static Config _config = Config.Create();
        private static ITorrentManager _torrentManager;
        private static FileManager _fileManager = new FileManager(_config);
        private static Unpacker unpacker = new Unpacker(_config, _fileManager);
        private static int processedTorrentsCount = 0;

        static void Main(string[] args)
        {
            try
            {
                _logger.Info("Application starting.");

                ValidateConfig();
                CreateTorrentManager();

                var torrents = _torrentManager.GetTorrentList();

                _logger.Info($"LOOPING TORRENTS");
                foreach (var torrent in torrents)
                {
                    LogStartProcessTorrent(torrent);
                    HandleAlreadyProcessedTorrent(torrent);
                    HandleUnprocessedTorrent(torrent);
                }
                _logger.Info($"FINISHED");

                //if (ShouldProcessAllTorrents())
                //    ProcessAllTorrents();

                //if (ShouldProcessOneTorrent())
                //    ProcessOneTorrent();
            }
            catch (Exception ex)
            {
                _logger.Fatal(ex, "Unexpected error occurred.");
            }
            WaitAndExit();
        }

        private static void HandleUnprocessedTorrent(Torrent torrent)
        {
            if (!_torrentManager.HasTorrentBeenPostProcessed(torrent))
            {
                if (processedTorrentsCount >= _config.MaxProcessTorrentsInBatch)
                {
                    // Enough in this batch already. Exit.
                    _logger.Info($"- Already Processed {processedTorrentsCount} in this batch which was the configured max, exiting..");
                    WaitAndExit();
                }

                // Otherwise, lets unpack/process!
                processedTorrentsCount++;

                // Mark as in progress
                _torrentManager.MarkTorrentAsProcessing(torrent);

                var unpackedOk = unpacker.CopyAndUnpack(torrent);
                if (unpackedOk)
                {
                    // Mark torrent as finished
                    _torrentManager.MarkTorrentAsProcessFinished(torrent);

                    _logger.Info($"- Torrent unpacked OK.");

                    if (_torrentManager.HasTorrentBeenPostProcessed(torrent) && _torrentManager.HasTorrentGoalsBeenReached(torrent))
                    {
                        // Delete if goals reached and torrent processed ok, if configured as such.
                        if (_config.DeleteFromTorrentsFolderWhenUnpacked)
                            _torrentManager.DeleteTorrent(torrent);
                    }
                }
                else
                {
                    // Unpack error!! Quit!
                    _logger.Error($"- Failed to unpack!!");
                    _torrentManager.MarkTorrentAsProcessFailed(torrent);
                    WaitAndExit();
                }
            }
        }

        private static void HandleAlreadyProcessedTorrent(Torrent torrent)
        {
            if (_torrentManager.HasTorrentBeenPostProcessed(torrent))
            {
                // Torrent has already been processed.
                _logger.Info($"- Torrent has already been processed!");
                if (_config.DeleteAlreadyProcessedTorrents)
                {
                    // Delete already processed torrents was true, so delete it.
                    _torrentManager.DeleteTorrent(torrent);
                }
            }
        }

        private static void LogStartProcessTorrent(Torrent torrent)
        {
            _logger.Info($"Processing: {torrent.Name} -");
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

        private static void ProcessOneTorrent()
        {
            //TODO: Support argument containing hash!
            var torrent = new Torrent("....", _config);
            _logger.Info($"Torrent hash is: {torrent.Hash}. I will now load all torrents.");

            var torrents = _torrentManager.GetTorrentList();
            torrent = torrents.Single(p => p.Hash == torrent.Hash);
            _logger.Info($"Found torrent. Name: {torrent.Name}, NumericStatus: {torrent.NumericStatus}, Status: {torrent.TorrentStatus}");

            if (!_torrentManager.HasTorrentBeenPostProcessed(torrent))
            {
                // Execute PostProcess
                // TODO: Create method and move logging here
                _logger.Info("Post processes starting.");
            }

            if (_torrentManager.HasTorrentBeenPostProcessed(torrent) && _torrentManager.HasTorrentGoalsBeenReached(torrent))
            {
                // Delete
                // TODO: Create method and move logging here
                _logger.Info("Deleting torrent, goals has been reached.");
            }
            else
            {
                _logger.Warn($"Torrent has either not been post processed yet, or the goals haven't been reached. Not doing anything.");
            }
        }

        static void UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs e)
        {

        }

        private static void WaitAndExit()
        {
            Console.WriteLine("-- Finished, press any key to exit --");
            Console.ReadKey();
            Environment.Exit(1);
        }

    }
}
